using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Threading;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ManagedFusion.Crawler
{
	class Program
	{
		private static int CurrentlyProcessingStart = 0;
		private static string LookingForDomain = null;
		private static List<string> ResultsFound = new List<string>();
		private static readonly Regex GoogleUrlResultSize = new Regex(@" - [0-9]+k - ", RegexOptions.Compiled);

		[STAThread]
		private static void Main(string[] args)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write("Enter Domain: ");
			Console.ForegroundColor = ConsoleColor.Green;
			LookingForDomain = Console.ReadLine();

			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write("Select Keywords: ");
			Console.ForegroundColor = ConsoleColor.Green;
			string keywords = Console.ReadLine();

			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write("Number of Results to Search: ");
			Console.ForegroundColor = ConsoleColor.Green;
			string sresultsToSearch = Console.ReadLine();
			int ressultsToSearch;

			if (!Int32.TryParse(sresultsToSearch, out ressultsToSearch))
				ressultsToSearch = 200;

			for (int i = 0; i < ressultsToSearch; i = i + 10)
			{
				CurrentlyProcessingStart = i;

				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("Trying {0} to {1}", CurrentlyProcessingStart, CurrentlyProcessingStart + 10);
				Console.ResetColor();

				UriBuilder url = new UriBuilder("http", "www.google.com", 80, "search");
				url.Query = "num=10&lr=lang_en&safe=off&start=" + i + "&q=" + keywords;

				Robot bot = new Robot(url.Uri, Robot.MicrosoftInternetExplorer70UserAgent);
				bot.MaxProcessorsAllowed = 1;
				bot.ProcessSubPages = false;
				bot.UriProcessingFinished += new EventHandler<UriProcessingFinishedEventArgs>(bot_UriProcessingFinished);
				bot.Scan();
			}

			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write("Do you want to save these results? ");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.Beep();
			string saveResults = Console.ReadLine();

			if (!String.IsNullOrEmpty(saveResults) && saveResults.ToLower()[0] == 'y')
			{
				SaveFileDialog saveDialog = new SaveFileDialog();
				saveDialog.Filter = "Text File (*.txt)|*.txt";
				saveDialog.Title = "Where do you want to save the results?";
				saveDialog.AddExtension = true;
				saveDialog.SupportMultiDottedExtensions = true;
				saveDialog.OverwritePrompt = true;
				saveDialog.AutoUpgradeEnabled = true;
				saveDialog.DefaultExt = "txt";

				if (saveDialog.ShowDialog() == DialogResult.OK)
				{
					using (var writer = new StreamWriter(saveDialog.OpenFile()))
					{
						writer.WriteLine("Domain: " + LookingForDomain);
						writer.WriteLine("Keywords: " + keywords);
						writer.WriteLine();

						foreach (string result in ResultsFound)
							writer.WriteLine(result);
					}
				}
			}

			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("Processing Done");

			Console.Read();
		}

		private static void bot_UriProcessingFinished(object sender, UriProcessingFinishedEventArgs e)
		{
			Sgml.SgmlReader sgmlReader = new Sgml.SgmlReader();
			sgmlReader.DocType = "HTML";
			sgmlReader.WhitespaceHandling = WhitespaceHandling.All;
			sgmlReader.CaseFolding = Sgml.CaseFolding.ToLower;
			sgmlReader.InputStream = new StringReader(e.Content);

			XmlDocument doc = new XmlDocument();
			doc.PreserveWhitespace = true;
			doc.XmlResolver = null;
			doc.Load(sgmlReader);

			XmlNodeList list = doc.SelectNodes(@"/html/body[@id='gsr']/div[@id='res']/div/ol/li/div/cite");
			int count = 0;

			foreach (XmlNode node in list)
			{
				count++;
				string foundUrl = node.InnerText;
				foundUrl = GoogleUrlResultSize.Replace(foundUrl, String.Empty);
				foundUrl = "http://" + foundUrl;
				Uri url;

				if (Uri.TryCreate(foundUrl, UriKind.Absolute, out url))
				{
					if (url.Host.IndexOf(LookingForDomain) >= 0)
					{
						string result = String.Format("Rank {0} for {1}", count + CurrentlyProcessingStart, url);
						ResultsFound.Add(result);

						Console.ForegroundColor = ConsoleColor.Cyan;
						Console.WriteLine(result);
						Console.ResetColor();
						Console.Beep();
					}
				}
			}
		}
	}
}
