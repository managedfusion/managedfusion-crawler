using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Threading;

namespace ManagedFusion.Crawler
{
	class Program
	{
		[STAThread]
		private static void Main(string[] args)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write("Enter Domain: ");
			Console.ForegroundColor = ConsoleColor.Green;
			string url = Console.ReadLine();

			if (!url.StartsWith("http://"))
				url = "http://" + url;

			var dc = new CrawlerDataContext();
			var query = from s in dc.Sessions
						where s.Url == url
						orderby s.ScanDate descending
						select s;

			var sessions = query.ToList();
			for(int i = 0; i < sessions.Count; i++)
			{
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.Write("    [" + i + "]  ");
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("{0:d}", sessions[i].ScanDate);
			}

			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write("Select Session: ");
			Console.ForegroundColor = ConsoleColor.Green;
			string csession = Console.ReadLine();
			int session;

			if (!Int32.TryParse(csession, out session))
				session = 0;

			var selectedSession = sessions[session];

			SaveFileDialog saveDialog = new SaveFileDialog();
			saveDialog.Filter = "Sitemap File (*.xml)|*.xml";
			saveDialog.Title = "Where do you want to save this sitemap?";
			saveDialog.AddExtension = true;
			saveDialog.SupportMultiDottedExtensions = true;
			saveDialog.OverwritePrompt = true;
			saveDialog.AutoUpgradeEnabled = true;
			saveDialog.DefaultExt = "xml";

			if (saveDialog.ShowDialog() == DialogResult.OK)
			{
				using (var writer = new StreamWriter(saveDialog.OpenFile()))
				{
					writer.WriteLine(@"<?xml version=""1.0"" encoding=""UTF-8"" ?>");
					writer.WriteLine(@"<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">");

					var query2 = from s in selectedSession.SessionScans
								 where s.Status == 200 && s.ContentType != null && s.ContentType.Contains("html")
								 select s.Url;

					foreach (var scan in query2)
					{
						Console.ForegroundColor = ConsoleColor.Yellow;
						Console.WriteLine(scan);

						writer.WriteLine("\t<url>");
						writer.WriteLine("\t\t<loc>{0}</loc>", scan);
						writer.WriteLine("\t</url>");
					}

					writer.WriteLine("</urlset>");
				}
			}

			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("Processing Done");

			Console.Read();
		}
	}
}
