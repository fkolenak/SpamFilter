using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace jschropf.Bayesian
{
	/// <summary>
    /// cs obsahujici korpus pro spam filter (slova a jejich cetnosti)
	/// From: http://www.paulgraham.com/spam.html
	/// </summary>
	public class Korpus
	{
		/// <summary>
		/// regex pro slova co nezacinaji cislem
		/// </summary>
		public const string TokenPattern = @"([a-zA-Z]\w+)\W*";

		private SortedDictionary<string, int> _tokeny = new SortedDictionary<string, int>();


		/// <summary>
        /// Serazeny list vsech slov v textu s jejich cetnostmi
		/// </summary>
		public SortedDictionary<string, int> Tokeny
		{
			get { return _tokeny; }
		}

		/// <summary>
        /// konstruktor
		/// </summary>
		public Korpus()
		{
		}

		/// <summary>
		/// konstruktor na zaplneni korpusu z readeru
		/// </summary>
		/// <param name="reader"></param>
		public Korpus(TextReader reader)
		{
			NactiZReaderu(reader);
		}

		/// <summary>
		/// kostruktor na naplneni korpusu ze souboru
		/// </summary>
		/// <param name="cesta"></param>
		public Korpus(string cesta)
		{
			NactiZeSouboru(cesta);
		}

		/// <summary>
		/// Zaplneni korpusu textem ze souboru
		/// </summary>
		/// <param name="cesta"></param>
		public void NactiZeSouboru(string cesta)
		{
			NactiZReaderu(new StreamReader(cesta));
		}

		/// <summary>
        /// Nacte tokenu z textreaderu do korpusu
        /// neinicializuje kolekci, takze muze bezet ve smicce
		/// </summary>
		/// <param name="reader"></param>
		public void NactiZReaderu(TextReader reader)
		{
			Regex re = new Regex(TokenPattern, RegexOptions.Compiled);
			string radka;
			while (null != (radka = reader.ReadLine()))
			{
				Match m = re.Match(radka);
				while (m.Success)
				{
					string token = m.Groups[1].Value;
					PridejToken(token);
					m = m.NextMatch();
				}
			}
		}

		/// <summary>
        /// Prida slovo do listu nebo zvysi pocet vyskytu pokud uz tam je
		/// </summary>
		/// <param name="slovo"></param>
		public void PridejToken(string slovo)
		{
			if (!_tokeny.ContainsKey(slovo))
			{
				_tokeny.Add(slovo, 1);
			}
			else
			{
				_tokeny[slovo]++;
			}
		}


	}

}
