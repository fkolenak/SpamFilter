using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using jschropf.Bayesian;

namespace SpamFilterSample
{
	public partial class Form1 : Form
	{
		private SpamFilter _filter;

		public Form1()
		{
			InitializeComponent();
		}

		private void TestFile(string soubor)
		{
			if (_filter == null)
			{
				MessageBox.Show("Napred nactete data!");
				return;
			}

			string text = new StreamReader(soubor).ReadToEnd();
			txtOut.Text = soubor + Environment.NewLine + "score: " + _filter.Test(text).ToString();
			txtOut.AppendText(Environment.NewLine + Environment.NewLine + text);
		}

		#region tlacitka
		private void btnLoad_Click(object sender, EventArgs e)
		{
			Korpus spatny = new Korpus();
			Korpus dobry = new Korpus();
			spatny.NactiZeSouboru("../../TestData/spam.txt");
			dobry.NactiZeSouboru("../../TestData/good.txt");

			_filter = new SpamFilter();
			_filter.Nacti(dobry, spatny);


			// Vypis statistik o nactenych datech.
			txtOut.Text = String.Format(@"{0} {1} {2}{3}"
				, _filter.Dobre.Tokeny.Count
				, _filter.Spatne.Tokeny.Count
				, _filter.ListPravdepodobnosti.Count
				, Environment.NewLine);

			// Vypis pravdepodobnosti
			foreach (string key in _filter.ListPravdepodobnosti.Keys)
			{
				if (_filter.ListPravdepodobnosti[key] > 0.02)
				{
					txtOut.AppendText(String.Format("{0},{1}{2}", _filter.ListPravdepodobnosti[key].ToString(".0000"), key, Environment.NewLine));
				}
			}

		}

		private void btnUrciteDobry(object sender, EventArgs e)
		{
			TestFile("../../TestData/definatelyOK.txt");
		}

		private void btnMoznaSpam(object sender, EventArgs e)
		{
			TestFile("../../TestData/maybeSpam.txt");
		}

		private void btnUrciteSpam(object sender, EventArgs e)
		{
			TestFile("../../TestData/definatelySpam.txt");
		}

		private void btnTest(object sender, EventArgs e)
		{
			if (_filter == null)
			{
				MessageBox.Show("Load first!");
				return;
			}

			string body = txtOut.Text;
			txtOut.Text = "score: " + _filter.Test(body).ToString();
			txtOut.AppendText(Environment.NewLine + body);
		}

		private void btnDoSouboru(object sender, EventArgs e)
		{
			if (_filter == null)
			{
				MessageBox.Show("Load first!");
				return;
			}

			_filter.doSouboru("../../TestData/out.txt");
		}

		private void btnZeSouboru(object sender, EventArgs e)
		{
			_filter = new SpamFilter();
			_filter.zeSouboru("../../TestData/out.txt");
		}

		#endregion

        private void Form1_Load(object sender, EventArgs e)
        {

        }
	}
}