using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ManagedFusion.Crawler
{
	/// <summary>
	/// 
	/// </summary>
	public class UriHandler : IDisposable
	{
		private static Regex UriExpression = new Regex(@"<a[\s]+[^>]*?href[\s]*=[\s]*[\""\'](?'url'.*?)[\""\'].*?>(?'name'[^<]+|.*?)?<\/a>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		private UriElement _element;
		private HttpWebResponse _response;
		private List<UriElement> _related;

		/// <summary>
		/// Initializes a new instance of the <see cref="HttpUriHandler"/> class.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="response">The response.</param>
		public UriHandler(UriElement element, HttpWebResponse response)
		{
			if (response == null)
				throw new ArgumentNullException("response");

			if (element == null)
				throw new ArgumentNullException("element");

			_response = response;
			_element = element;
			_related = new List<UriElement>();
		}

		/// <summary>
		/// Occurs when [started].
		/// </summary>
		public event EventHandler<UriHandlerStartedEventArgs> Started;

		/// <summary>
		/// Occurs when [URI found].
		/// </summary>
		public event EventHandler<UriFoundEventArgs> UriFound;

		/// <summary>
		/// Occurs when [finished].
		/// </summary>
		public event EventHandler<UriProcessingFinishedEventArgs> Finished;

		/// <summary>
		/// Raises the <see cref="E:Started"/> event.
		/// </summary>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		protected virtual void OnStarted(UriHandlerStartedEventArgs e)
		{
			if (Started != null)
				Started(this, e);
		}

		/// <summary>
		/// Raises the <see cref="E:UriFound"/> event.
		/// </summary>
		/// <param name="e">The <see cref="ManagedFusion.Crawler.UriFoundEventArgs"/> instance containing the event data.</param>
		protected virtual void OnUriFound(UriFoundEventArgs e)
		{
			_related.Add(e.Element);

			if (UriFound != null)
				UriFound(this, e);
		}

		/// <summary>
		/// Raises the <see cref="E:Finished"/> event.
		/// </summary>
		/// <param name="e">The <see cref="ManagedFusion.Crawler.UriProcessFinishedEventArgs"/> instance containing the event data.</param>
		protected virtual void OnFinished(UriProcessingFinishedEventArgs e)
		{
			if (Finished != null)
				Finished(this, e);
		}

		/// <summary>
		/// Gets the element.
		/// </summary>
		/// <value>The element.</value>
		public UriElement Element
		{
			get { return _element; }
		}

		/// <summary>
		/// Processes this instance.
		/// </summary>
		public void Process()
		{
			UriHandlerStartedEventArgs startedEventArgs = new UriHandlerStartedEventArgs(Element);
			OnStarted(startedEventArgs);

			if (startedEventArgs.Cancel)
				return;

			string hash = null;
			string content = null;
			Uri redirect = null;
			Stopwatch stopwatch = new Stopwatch(); 

			// send a found event on a redirect location in the header
			if (Uri.TryCreate(_response.Headers[HttpResponseHeader.Location], UriKind.RelativeOrAbsolute, out redirect))
				OnUriFound(new UriFoundEventArgs(_element.BaseUri, redirect));

			stopwatch.Start();
			using (Stream responseStream = _response.GetResponseStream())
			{
				using (StreamReader reader = new StreamReader(responseStream))
				{
					content = reader.ReadToEnd();
					hash = content.ToHashString("SHA1");

					// remove all line breaks for data matching
					string matchData = content.Replace('\n', ' ');
					MatchCollection uriMatches = UriExpression.Matches(matchData);

					foreach (Match match in uriMatches)
					{
						if (match.Success)
						{
							Uri uri;

							if (Uri.TryCreate(match.Groups["url"].Value, UriKind.RelativeOrAbsolute, out uri))
								OnUriFound(new UriFoundEventArgs(_element.RequestedUri, uri));
						}
					}
				}

				responseStream.Close();
			}
			stopwatch.Stop();

			OnFinished(new UriProcessingFinishedEventArgs(Element, _response, _related.ToArray(), hash, content, stopwatch.Elapsed));
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </returns>
		public override string ToString()
		{
			return _response.ResponseUri.ToString();
		}

		#region IDisposable Members

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			if (_response != null)
				_response.Close();
		}

		#endregion
	}
}
