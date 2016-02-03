using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace jschropf.Bayesian
{
	/// <summary>
	/// Implementace naivniho Bayesova algoritmu podle:
	/// http://www.paulgraham.com/spam.html
	/// </summary>
	public class SpamFilter
	{
		#region atributy pro snazsi vypocet
		/// <summary>
        /// konstanty pro Bayesuv algoritmus
		/// </summary>
		public class ListAtributu
		{
														    // Hodnoty v originalnim clanku Paula Grahama:
			public int VahaDobrehoTokenu = 2;				// 2
			public int MinPocetTokenu = 0;				    // 0
			public int MinTokenuProPridani = 5;		        // 5
			public double MinSkore = 0.011;				    // 0.01
			public double MaxSkore = 0.99;				    // 0.99
			public double PravdepodobnySpam = 0.9998;		// 0.9998
			public double JistySpam = 0.9999;	            // 0.9999
			public int PocetProJistySpam = 10;			    // 10
			public int PocetZajimavychSlov = 20;	    	// 15 (pozdeji zmeneno na 20)
		}

		private ListAtributu _atributy = new ListAtributu();

		/// <summary>
        /// Atributy pro beh algoritmu
		/// </summary>
		public ListAtributu listAtributu
		{
			get { return _atributy; }
			set { _atributy = value; }
		}

		#endregion

		private Korpus _dobry;
		private Korpus _spatny;
		private SortedDictionary<string, double> _pravdepodobnosti;
		private int _pocetDobry;
		private int _pocetSpatny;

		#region vlastnosti
		/// <summary>
        /// List slov ktere se prevazne vyskytuji ve spamu
		/// </summary>
		public Korpus Spatne
		{
			get { return _spatny; }
			set { _spatny = value; }
		}

		/// <summary>
        /// List slov ktere se prevazne nevyskytuji ve spamu
		/// </summary>
		public Korpus Dobre
		{
			get { return _dobry; }
			set { _dobry = value; }
		}

		/// <summary>
        /// List pravdepodobnosti toho, ze se slovo vyskytne ve spamu
		/// </summary>
		public SortedDictionary<string, double> ListPravdepodobnosti
		{
			get { return _pravdepodobnosti; }
			set { _pravdepodobnosti = value; }
		}
		#endregion

		#region zaplneni

		/// <summary>
        /// Inicializuje SpamFilter podle dodaneho textu
		/// </summary>
		/// <param name="goodReader"></param>
		/// <param name="badReader"></param>
		public void Nacti(TextReader goodReader, TextReader badReader)
		{
			_dobry = new Korpus(goodReader);
			_spatny = new Korpus(badReader);

			SpoctiPravdepodobnosti();
		}

		/// <summary>
        /// Inicializuje SpamFilter podle obsahu korpusu
		/// </summary>
		/// <param name="dobry"></param>
		/// <param name="spatny"></param>
		public void Nacti(Korpus dobry, Korpus spatny)
		{
			_dobry = dobry;
			_spatny = spatny;

			SpoctiPravdepodobnosti();
		}

		/// <summary>
        /// Inicializuje SpamFilter podle DataTable obsahující sloupce "IsSpam" a "Body"
		/// </summary>
		/// <param name="tabulka"></param>
		public void Nacti(DataTable tabulka)
		{
			_dobry = new Korpus();
			_spatny = new Korpus();

			foreach (DataRow radek in tabulka.Rows)
			{
				bool jeSpam = (bool)radek["IsSpam"];
				string body = radek["Body"].ToString();
				if (jeSpam)
				{
					_spatny.NactiZReaderu(new StringReader(body));
				}
				else
				{
					_dobry.NactiZReaderu(new StringReader(body));
				}
			}

			SpoctiPravdepodobnosti();
		}

		/// <summary>
		/// vypocet pravdepodobnosti pro kolekci pravdepodobnosti
		/// </summary>
		private void SpoctiPravdepodobnosti()
		{
			_pravdepodobnosti = new SortedDictionary<string, double>();

			_pocetDobry = _dobry.Tokeny.Count;
			_pocetSpatny = _spatny.Tokeny.Count;
			foreach (string token in _dobry.Tokeny.Keys)
			{
				SpoctiPravdepodobostTokenu(token);
			}
			foreach (string token in _spatny.Tokeny.Keys)
			{
				if (!_pravdepodobnosti.ContainsKey(token))
				{
					SpoctiPravdepodobostTokenu(token);
				}
			}
		}

		/// <summary>
        /// Pro vybrany token spocte pravdepodobnost, ze se vyskytuje ve spamu tim,
        /// ze porovna pocet dobrych c spatnych textu ve kterych se jiz vyskytl
		/// </summary>
		/// <param name="token"></param>
		private void SpoctiPravdepodobostTokenu(string token)
		{
			/*
			 * Jde o implementaci Grahamova algoritmu z:
			 * http://www.paulgraham.com/spam.html
			 * 
			 *	(let ((g (* 2 (or (gethash word good) 0)))
			 *		  (b (or (gethash word bad) 0)))
			 *	   (unless (< (+ g b) 5)
			 *		 (max .01
			 *			  (min .99 (float (/ (min 1 (/ b nbad))
			 *								 (+ (min 1 (/ g ngood))   
			 *									(min 1 (/ b nbad)))))))))
			 */

			int d = _dobry.Tokeny.ContainsKey(token) ? _dobry.Tokeny[token] * listAtributu.VahaDobrehoTokenu : 0;
			int s = _spatny.Tokeny.ContainsKey(token) ? _spatny.Tokeny[token] : 0;

			if (d + s >= listAtributu.MinTokenuProPridani)
			{
				double dobryfaktor = Min(1, (double)d / (double)_pocetDobry);
				double spatnyfaktor = Min(1, (double)s / (double)_pocetSpatny);

				double pravdepodobnost = Max(listAtributu.MinSkore,
								Min(listAtributu.MaxSkore, spatnyfaktor / (dobryfaktor + spatnyfaktor))
							);

				// Specialni pripad kdyz se token vyskytuje jen ve spamu
				// .9998 pro tokeny jen ve spamu .9999 pokud se objevil vic jak 10krat
				if (d == 0)
				{
					pravdepodobnost = (s > listAtributu.PocetProJistySpam) ? listAtributu.JistySpam : listAtributu.PravdepodobnySpam;
				}

				_pravdepodobnosti[token] = pravdepodobnost;
			}
		}
#endregion

#region serialization
		/// <summary>
        /// ulozi list pravdepodobnosti do souboru
		/// </summary>
		/// <param name="cesta"></param>
		public void doSouboru(string cesta)
		{
			using (FileStream fs = new FileStream(cesta, FileMode.Create, FileAccess.Write))
			{
				StreamWriter writer = new StreamWriter(fs);

				writer.WriteLine(String.Format("{0},{1},{2}", _pocetDobry, _pocetSpatny, _pravdepodobnosti.Count));
				foreach (string key in _pravdepodobnosti.Keys)
				{
					writer.WriteLine(String.Format("{0},{1}", _pravdepodobnosti[key].ToString("#.#####"), key));
				}

				writer.Flush();
				fs.Close();
			}
		}

		/// <summary>
		/// Ziskani udaju ze souboru vytvorenym funkci doSouboru().
		/// </summary>
		/// <param name="filePath"></param>
		public void zeSouboru(string filePath)
		{
			_pravdepodobnosti = new SortedDictionary<string, double>();
			using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
			{
				StreamReader reader = new StreamReader(fs);
				
				ParseVyskytu(reader.ReadLine());

				while (!reader.EndOfStream)
				{
					ParsePravdepodobnosti(reader.ReadLine());
				}

				fs.Close();
			}
		}

		private void ParseVyskytu(string radka)
		{
			string[] tokens = radka.Split(',');
			if (tokens.Length > 1)
			{
				_pocetDobry = Convert.ToInt32(tokens[0]);
				_pocetSpatny = Convert.ToInt32(tokens[1]);
			}
		}

		private void ParsePravdepodobnosti(string radka)
		{
			string[] tokens = radka.Split(',');
			if (tokens.Length > 1)
			{
				_pravdepodobnosti.Add(tokens[1], Convert.ToDouble(tokens[0]));
			}
		}

#endregion

		#region testSpamu
		/// <summary>
		/// Vraci pravdepodobnost ze se jedna o spam
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public double Test(string text)
		{
			SortedList pravdepodobnosti = new SortedList();

			// Projdi vsechny slova v textu a zjisti jejich pravdepodobnost vyskytu spamu.
			// Udrzuje list sestupne podle "zajimavosti"
			Regex re = new Regex(Korpus.TokenPattern, RegexOptions.Compiled);
			Match m = re.Match(text);
			int index=0;
			while (m.Success)
			{
				string token = m.Groups[1].Value;
				if (_pravdepodobnosti.ContainsKey(token))
				{
					double pravdepodobnost = _pravdepodobnosti[token];
					string vypocet = (0.5 - Math.Abs(0.5 - pravdepodobnost)).ToString(".00000") + token + index++;
					pravdepodobnosti.Add(vypocet, pravdepodobnost);

				}

				m = m.NextMatch();
			}

			/* Zkombinuje 20 nejzajimavejsich pravdepodobnosti do jedne
			 * podle algoritmu z:
			 * http://www.paulgraham.com/naivebayes.html
			 * 
			 *				abc           
			 *	---------------------------
			 *	abc + (1 - a)(1 - b)(1 - c)
			 *
			 */

			double nasob = 1;  // abc..n
			double komb = 1;  // (1 - a)(1 - b)(1 - c)..(1-n)
			index = 0;
			foreach (string key in pravdepodobnosti.Keys)
			{
				double pravdepodobnost = (double)pravdepodobnosti[key];
				nasob = nasob * pravdepodobnost;
				komb = komb * (1 - pravdepodobnost);

				Debug.WriteLine(index + " " + pravdepodobnosti[key] + " " + key );

				if (++index > listAtributu.PocetZajimavychSlov)
					break;
			}

			return nasob / (nasob + komb);

		}
		#endregion

		#region pomocneFunkce

		private double Max(double prvni, double druhy)
		{
			return prvni > druhy ? prvni : druhy;
		}

		private double Min(double prvni, double druhy)
		{
			return prvni < druhy ? prvni : druhy;
		}
		#endregion
	}
}
