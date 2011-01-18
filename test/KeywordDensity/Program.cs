using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Threading;
using System.Text.RegularExpressions;

namespace ManagedFusion.Crawler
{
	class Program
	{
		private static string[] Keywords;
		private static List<string> ResultsFound = new List<string>();

		private static void Main(string[] args)
		{
			Uri url = null;
			bool goodUrl = false;

			while (!goodUrl)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Write("Enter URL: ");
				Console.ForegroundColor = ConsoleColor.Green;
				string surl = Console.ReadLine();

				goodUrl = Uri.TryCreate(surl, UriKind.Absolute, out url);

				if (!goodUrl)
				{
					Console.ForegroundColor = ConsoleColor.DarkRed;
					Console.WriteLine("The URL is not valid, please try again.");
				}
			}

			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write("Select Keywords (Seperate With Comma): ");
			Console.ForegroundColor = ConsoleColor.Green;
			string keywords = Console.ReadLine();

			Keywords = keywords.Split(',');

			Robot bot = new Robot(url, Robot.MicrosoftInternetExplorer70UserAgent);
			bot.MaxProcessorsAllowed = 1;
			bot.ProcessSubPages = false;
			bot.UriProcessingFinished += new EventHandler<UriProcessingFinishedEventArgs>(bot_UriProcessingFinished);
			bot.Scan();

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

			string textOnly = doc.DocumentElement.InnerText;

			foreach (string keyword in Keywords)
			{
				MatchCollection matches = Regex.Matches(textOnly, "(?'found'" + keyword.Replace(" ", "[\\s]*") + ")", RegexOptions.IgnoreCase);
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Write("Found ");
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.Write(keyword);
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Write(" in ");
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.Write(matches.Count);
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine(" different places.");
			}
		}
	}
}
