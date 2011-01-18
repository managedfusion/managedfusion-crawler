using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;

namespace ManagedFusion.Crawler
{
	class Program
	{
		private static Guid SessionKey;
		private static List<string> Errors;
		private static int UriFoundCount;

		private static void Main(string[] args)
		{
			SessionKey = Guid.NewGuid();
			Errors = new List<string>();
			UriFoundCount = 0;

			// set number of connections allowed to recommend limit
			// <see href="http://support.microsoft.com/kb/821268" />
			ServicePointManager.DefaultConnectionLimit = 12 * Environment.ProcessorCount;

			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write("Enter Domain: ");
			Console.ForegroundColor = ConsoleColor.Green;
			string url = Console.ReadLine();

			if (!url.StartsWith("http://"))
				url = "http://" + url;
			
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write("Enter Number of Processors (" + 12 * Environment.ProcessorCount + " recommended): ");
			Console.ForegroundColor = ConsoleColor.Green;
			string cprocessors = Console.ReadLine();
			int processors;

			if (!Int32.TryParse(cprocessors, out processors))
				processors = 1;

			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine("Starting scan of {0} with {1} thread{2}", url, processors, processors == 1 ? String.Empty : "s");
			Console.ResetColor();

			using (var dc = new CrawlerDataContext())
			{
				Session session = new Session {
					SessionKey = SessionKey,
					ScanDate = DateTime.UtcNow,
					Url = url
				};

				dc.Sessions.InsertOnSubmit(session);
				dc.SubmitChanges();
			}

			Robot bot = new Robot(new Uri(url));
			bot.MaxProcessorsAllowed = processors;
			bot.UriProcessingFinished += new EventHandler<UriProcessingFinishedEventArgs>(bot_UriProcessingFinished);
			bot.UriFound += new EventHandler<UriFoundEventArgs>(bot_UriFound);
			bot.Scan();

			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("Processing Done");

			if (Errors.Count > 0)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Errors");
				Console.ResetColor();

				for (int i = 0; i < Errors.Count; i++)
				{
					if (i % 2 == 0)
						Console.ForegroundColor = ConsoleColor.Red;

					Console.WriteLine(Errors[i]);
					Console.ResetColor();
				}
			}

			Console.Read();
		}

		private static void bot_UriFound(object sender, UriFoundEventArgs e)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(e.Element.RequestedUri);
			Console.ResetColor();
		}

		private static Regex TitleExpression = new Regex(@"<title>(?'title'.*)</title>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static Regex MetaExpression = new Regex(@"<meta([\s]+[^>]*?name[\s]*=[\s]*[\""\'](?'name'.*?)[\""\'])?[\s]+[^>]*?content[\s]*=[\s]*[\""\'](?'content'.*?)[\""\']([\s]+[^>]*?name[\s]*=[\s]*[\""\'](?'name'.*?)[\""\'])?.*?/?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		private static void bot_UriProcessingFinished(object sender, UriProcessingFinishedEventArgs e)
		{
			Robot robot = sender as Robot;

			if (robot != null)
				Console.WriteLine("Done: {0} Threads: {1} Processing: {2} To Go: {3}", robot.ProcessedCount, robot.ActiveThreadCount, robot.ProcessingCount, robot.NotProcessedCount);

			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine(e.Element.RequestedUri.ToString());
			Console.ResetColor();

			UriFoundCount++;

			if (e.Status >= 500)
			{
				string path = String.Format(@"c:\crawler\{0}\{1}.html", e.Element.BaseUri, e.ContentHash);

				if (!File.Exists(path))
				{
					using (StreamWriter writer = File.CreateText(path))
					{
						writer.Write(e.Content);
					}
				}
			}

			string title = null;
			string description = null;
			string keywords = null;
			string robots = null;
			string matchData = e.Content;

			Match titleMatch = TitleExpression.Match(matchData);

			if (titleMatch.Success)
				title = titleMatch.Groups["title"].Value.Trim();

			MatchCollection metaMatches = MetaExpression.Matches(matchData);

			foreach (Match match in metaMatches)
			{
				if (match.Success)
				{
					if (String.Compare(match.Groups["name"].Value, "description", true) == 0)
						description = match.Groups["content"].Value.Trim();
					else if (String.Compare(match.Groups["name"].Value, "keywords", true) == 0)
						keywords = match.Groups["content"].Value.Trim();
					else if (String.Compare(match.Groups["name"].Value, "robots", true) == 0)
						robots = match.Groups["content"].Value.Trim();
				}
			}

			try
			{
				using (CrawlerDataContext dc = new CrawlerDataContext())
				{
					SessionScan scan = new SessionScan {
						SessionKey = SessionKey,
						UrlHash = e.Element.RequestedUri.ToString().ToHashString("SHA1"),
						ContentHash = e.ContentHash,
						ScanDate = DateTime.UtcNow,
						Host = e.Element.RequestedUri.Host,
						Base = e.Element.BaseUri.OriginalString,
						Found = e.Element.FoundUri.OriginalString,
						Url = e.Element.RequestedUri.OriginalString,
						Redirect = e.ResponseHeaders[HttpResponseHeader.Location],
						Method = e.Method,
						Status = e.Status,
						Title = title,
						Description = description,
						Keywords = keywords,
						Robots = ProcessRobots(robots, e).ToString(),
						ContentType = e.ResponseHeaders[HttpResponseHeader.ContentType],
						ContentEncoding = e.ResponseHeaders[HttpResponseHeader.ContentEncoding],
						ContentLength = TryConvertInt64(e.ResponseHeaders[HttpResponseHeader.ContentLength]),
						CacheControl = e.ResponseHeaders[HttpResponseHeader.CacheControl],
						Expires = e.ResponseHeaders[HttpResponseHeader.Expires]
					};

					Dictionary<string, SessionScanRelation> relatedUrls = new Dictionary<string, SessionScanRelation>(e.Related.Length);

					// remove duplicates
					foreach (UriElement related in e.Related)
					{
						string relatedHash = related.RequestedUri.ToString().ToHashString("SHA1");

						if (relatedUrls.ContainsKey(relatedHash))
							relatedUrls[relatedHash].Count++;
						else
							relatedUrls.Add(relatedHash, new SessionScanRelation {
								SessionKey = SessionKey,
								UrlHash = e.Element.RequestedUri.ToString().ToHashString("SHA1"),
								RelatedHash = relatedHash,
								Related = related.RequestedUri.ToString(),
								Count = 1
							});
					}

					// add all the related urls to the scan
					scan.SessionScanRelations.AddRange(relatedUrls.Values);

					dc.SessionScans.InsertOnSubmit(scan);
					dc.SubmitChanges();
				}
			}
			catch (Exception exc)
			{
				if (!Errors.Contains(exc.Message))
					Errors.Add(exc.Message);

				Console.BackgroundColor = ConsoleColor.Red;
				Console.WriteLine(exc.Message);
				Console.ResetColor();
			}
		}

		/// <summary>
		/// Tries the convert int64.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns></returns>
		private static long? TryConvertInt64(string value)
		{
			long output;

			if (Int64.TryParse(value, out output))
				return output;

			return null;
		}

		private static RobotTag ProcessRobots(string metaRobots, UriProcessingFinishedEventArgs e)
		{
			RobotTag robots = RobotTag.Null;
			List<string> robotParts = new List<string>();

			if (metaRobots != null)
				robotParts.AddRange(metaRobots.ToLower().Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries));

			if (e.ResponseHeaders["X-Robots-Tag"] != null)
				robotParts.AddRange(e.ResponseHeaders["X-Robots-Tag"].ToLower().Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries));

			for (int i = 0; i < robotParts.Count; i++)
				robotParts[i] = robotParts[i].Trim();

			if (robotParts.Contains("index"))
				robots |= RobotTag.Index;

			if (robotParts.Contains("noindex"))
				robots |= RobotTag.NoIndex;

			if (robotParts.Contains("follow"))
				robots |= RobotTag.Follow;

			if (robotParts.Contains("nofollow"))
				robots |= RobotTag.NoFollow;

			if (robotParts.Contains("all"))
				robots |= RobotTag.Index | RobotTag.Follow;

			if (robotParts.Contains("none"))
				robots |= RobotTag.NoIndex | RobotTag.NoFollow;

			if (robotParts.Contains("noarchive"))
				robots |= RobotTag.NoArchive;

			if (robotParts.Contains("nosnippet"))
				robots |= RobotTag.NoSnippet;

			if (robotParts.Contains("unavailable_after"))
				robots |= RobotTag.UnavailableAfter;

			// remove null if other robot tags were found
			if (robots != RobotTag.Null)
				robots &= ~RobotTag.Null;

			return robots;
		}
	}
}
