using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Collections;

namespace ManagedFusion.Crawler
{
	/// <summary>
	/// The Crawler Robot that will scan the URI that it is given.
	/// </summary>
	public sealed class Robot
	{
		/// <summary>
		/// 
		/// </summary>
		private delegate void ScanDelegate();

		/// <summary>
		/// The ManagedFusion User-Agent of the <see cref="Robot"/>.
		/// </summary>
		public const string ManagedFusionUserAgent = @"ManagedFusion/1.0 (+http://managedfusion.com/bot.html)";

		/// <summary>
		/// The Google User-Agent of the <see cref="Robot"/>.
		/// </summary>
		public const string GoogleUserAgent = @"Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)";

		/// <summary>
		/// The Microsoft Internet Explorer 7.0 User-Agent of the <see cref="Robot"/>.
		/// </summary>
		public const string MicrosoftInternetExplorer70UserAgent = @"Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.0)";

		/// <summary>
		/// The method used in the HTTP Request.
		/// </summary>
		public const string RequestMethod = "GET";

		/// <summary>
		/// The default max processors allowed.
		/// </summary>
		private const int DefaultMaxProcessorsAllowed = 5;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		private Queue<UriElement> _notProcessed;
		
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		private List<UriElement> _processed;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		private Dictionary<UriElement, DateTime> _processing;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private int _maxProcessorsAllowed;

		[NonSerialized, DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private object _sync;

		[NonSerialized, DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private int _asyncActiveCount;

		[NonSerialized, DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private AutoResetEvent _asyncActiveEvent;

		[NonSerialized, DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private ScanDelegate _scanDelegate;

		[NonSerialized, DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private Thread[] _threadPool;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="Robot"/> class.
		/// </summary>
		/// <param name="uri">The URI.</param>
		public Robot(Uri uri) : this(uri, ManagedFusionUserAgent) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Robot"/> class.
		/// </summary>
		/// <param name="uri">The URI.</param>
		/// <param name="userAgent">The user agent.</param>
		public Robot(Uri uri, string userAgent)
		{
			if (!uri.IsAbsoluteUri)
				throw new ArgumentException("'uri' must be an absolute URI.", "uri");

			_notProcessed = new Queue<UriElement>();
			_processed = new List<UriElement>();
			_processing = new Dictionary<UriElement, DateTime>(); ;

			InitialUrl = uri;
			UserAgent = userAgent;
			ProcessSubDomains = true;
			ProcessSubPages = true;
			AllowMultipleWebRequests = false;
			MaxProcessorsAllowed = 5;
			MaxTimeAllowedToProcess = new TimeSpan(0, 5, 0);

			_sync = new object();
			_notProcessed.Enqueue(new UriElement(InitialUrl, InitialUrl));
			_asyncActiveCount = 1;
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when [started].
		/// </summary>
		public event EventHandler Started;

		/// <summary>
		/// Occurs when [finished].
		/// </summary>
		public event EventHandler Finished;

		/// <summary>
		/// Occurs when [URI processing finished].
		/// </summary>
		public event EventHandler<UriProcessingFinishedEventArgs> UriProcessingFinished;

		/// <summary>
		/// Occurs when [URI found].
		/// </summary>
		public event EventHandler<UriFoundEventArgs> UriFound;

		/// <summary>
		/// Occurs when [URI error].
		/// </summary>
		public event EventHandler<UriFoundEventArgs> UriError;

		/// <summary>
		/// Raises the <see cref="E:Started"/> event.
		/// </summary>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void OnStarted(EventArgs e)
		{
			if (Started != null)
				Started(this, e);
		}

		/// <summary>
		/// Raises the <see cref="E:Finished"/> event.
		/// </summary>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void OnFinished(EventArgs e)
		{
			if (Finished != null)
				Finished(this, e);
		}

		/// <summary>
		/// Raises the <see cref="E:UriProcessingFinished"/> event.
		/// </summary>
		/// <param name="e">The <see cref="ManagedFusion.Crawler.UriProcessFinishedEventArgs"/> instance containing the event data.</param>
		private void OnUriProcessingFinished(UriProcessingFinishedEventArgs e)
		{
			if (UriProcessingFinished != null)
				UriProcessingFinished(this, e);
		}

		/// <summary>
		/// Raises the <see cref="E:UriFound"/> event.
		/// </summary>
		/// <param name="e">The <see cref="ManagedFusion.Crawler.UriFoundEventArgs"/> instance containing the event data.</param>
		private void OnUriFound(UriFoundEventArgs e)
		{
			if (UriFound != null)
				UriFound(this, e);
		}

		/// <summary>
		/// Raises the <see cref="E:UriError"/> event.
		/// </summary>
		/// <param name="e">The <see cref="ManagedFusion.Crawler.UriFoundEventArgs"/> instance containing the event data.</param>
		private void OnUriError(UriFoundEventArgs e)
		{
			if (UriError != null)
				UriError(this, e);
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the not processed count.
		/// </summary>
		/// <value>The not processed count.</value>
		public int NotProcessedCount
		{
			get { return _notProcessed.Count; }
		}

		/// <summary>
		/// Gets the processed count.
		/// </summary>
		/// <value>The processed count.</value>
		public int ProcessedCount
		{
			get { return _processed.Count; }
		}

		/// <summary>
		/// Gets the processing count.
		/// </summary>
		/// <value>The processing count.</value>
		public int ProcessingCount
		{
			get { return _processing.Count; }
		}

		/// <summary>
		/// Gets the threads in use.
		/// </summary>
		/// <value>The threads in use.</value>
		public int ActiveThreadCount
		{
			get
			{
				int count = 0;

				for (int i = 0; i < _threadPool.Length; i++)
					if (_threadPool[i] != null && _threadPool[i].IsAlive)
						count++;

				return count;
			}
		}

		/// <summary>
		/// Gets the initial URL.
		/// </summary>
		/// <value>The initial URL.</value>
		public Uri InitialUrl { get; private set; }

		/// <summary>
		/// Gets or sets the user agent.
		/// </summary>
		/// <value>The user agent.</value>
		public string UserAgent { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether [process sub domains].
		/// </summary>
		/// <value><c>true</c> if [process sub domains]; otherwise, <c>false</c>.</value>
		public bool ProcessSubDomains { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether [process sub pages].
		/// </summary>
		/// <value><c>true</c> if [process sub pages]; otherwise, <c>false</c>.</value>
		public bool ProcessSubPages { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether if multiple HTTP requests are allowed to be executed at the same time.
		/// </summary>
		/// <value>
		/// 	<see langword="true"/> if multiple HTTP requests are allowed; otherwise, <see langword="false"/>.
		/// </value>
		/// <remarks>This enables threading in the <see cref="Robot"/> when sending HTTP web requests.</remarks>
		public bool AllowMultipleWebRequests
		{
			get { return MaxProcessorsAllowed > 1; }
			set { MaxProcessorsAllowed = value ? DefaultMaxProcessorsAllowed : 1; }
		}

		/// <summary>
		/// Gets or sets the max processors allowed.
		/// </summary>
		/// <value>The max processors allowed.</value>
		public int MaxProcessorsAllowed
		{
			get { return _maxProcessorsAllowed; }
			set { _maxProcessorsAllowed = Math.Max(value, 1); }
		}

		/// <summary>
		/// Gets or sets the max time allowed to process.
		/// </summary>
		/// <value>The max time allowed to process.</value>
		public TimeSpan MaxTimeAllowedToProcess { get; set; }

		/// <summary>
		/// Gets a value indicating whether this instance is thread available.
		/// </summary>
		/// <value>
		/// 	<see langword="true"/> if this instance is thread available; otherwise, <see langword="false"/>.
		/// </value>
		private bool IsThreadAvailable
		{
			get
			{
				for (int i = 0; i < _threadPool.Length; i++)
					if (_threadPool[i] == null || !_threadPool[i].IsAlive)
						return true;

				return false;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance is any thread alive.
		/// </summary>
		/// <value>
		/// 	<see langword="true"/> if this instance is any thread alive; otherwise, <see langword="false"/>.
		/// </value>
		private bool IsAnyThreadAlive
		{
			get { return ActiveThreadCount > 0; }
		}

		#endregion

		/// <summary>
		/// Start scan with this instance.
		/// </summary>
		public void Scan()
		{
			// create thread pool
			_threadPool = new Thread[MaxProcessorsAllowed];

			// announce scan started
			OnStarted(EventArgs.Empty);
	
			// process the first uri with out threading
			UriElement startingUri = _notProcessed.Dequeue();
			ProcessHandler(startingUri);

			// loop through the sub pages if they are suppose to be processed
			if (ProcessSubPages)
			{
				// jump in to look where threading is used if set
				while (_notProcessed.Count > 0 || _processing.Count > 0 || IsAnyThreadAlive)
				{
					if (_notProcessed.Count == 0 || (AllowMultipleWebRequests && !IsThreadAvailable))
					{
						CleanUpProcessing();
						Thread.Sleep(1000);
						continue;
					}

					UriElement uri = _notProcessed.Dequeue();

					// thread this app if requested
					if (AllowMultipleWebRequests)
					{
						bool threaded = ThreadProcessHandler(uri);

						if (!threaded)
							_notProcessed.Enqueue(uri);
					}
					else
						ProcessHandler(uri);
				}
			}

			// announce scan finished
			OnFinished(EventArgs.Empty);
		}

		/// <summary>
		/// Begins the scan.
		/// </summary>
		/// <param name="callback">The callback.</param>
		/// <param name="state">The state.</param>
		/// <returns></returns>
		public IAsyncResult BeginScan(AsyncCallback callback, object state)
		{
			Interlocked.Increment(ref this._asyncActiveCount);
			ScanDelegate scanDelegate = new ScanDelegate(this.Scan);
			
			if (_asyncActiveEvent == null)
			{
				lock (this)
				{
					if (_asyncActiveEvent == null)
						_asyncActiveEvent = new AutoResetEvent(true);
				}
			}
			
			_asyncActiveEvent.WaitOne();
			_scanDelegate = scanDelegate;

			return scanDelegate.BeginInvoke(callback, state);
		}

		/// <summary>
		/// Ends the scan.
		/// </summary>
		/// <param name="result">The result.</param>
		public void EndScan(IAsyncResult result)
		{
			if (result == null)
				throw new ArgumentNullException("asyncResult");

			if (_scanDelegate == null)
				throw new InvalidOperationException("A async scan was never started.");
			
			try
			{
				_scanDelegate.EndInvoke(result);
			}
			finally
			{
				_scanDelegate = null;
				_asyncActiveEvent.Set();

				if ((this._asyncActiveEvent != null) && (Interlocked.Decrement(ref this._asyncActiveCount) == 0))
				{
					this._asyncActiveEvent.Close();
					this._asyncActiveEvent = null;
				}
			}
		}

		/// <summary>
		/// Cleans the up processing.
		/// </summary>
		private void CleanUpProcessing()
		{
			List<UriElement> list = new List<UriElement>();
			Dictionary<UriElement, DateTime> processing = new Dictionary<UriElement, DateTime>(_processing);

			// find all URI's that have exceeded their max time allowed to process
			foreach (KeyValuePair<UriElement, DateTime> pair in processing)
				if (DateTime.Now - pair.Value >= MaxTimeAllowedToProcess)
					list.Add(pair.Key);

			foreach (UriElement uri in list)
			{
				_processing.Remove(uri);

				// if it hasn't been processed yet enqueue the uri for processing
				if (!IsProcessed(uri))
					_notProcessed.Enqueue(uri);
			}
		}

		/// <summary>
		/// Threads the process handler.
		/// </summary>
		/// <param name="uri">The URI.</param>
		/// <returns></returns>
		private bool ThreadProcessHandler(UriElement uri)
		{
			// find a free slot in the thread pool
			for (int i = 0; i < _threadPool.Length; i++)
			{
				if (_threadPool[i] == null || !_threadPool[i].IsAlive)
				{
					ParameterizedThreadStart start = new ParameterizedThreadStart(this.ProcessHandler);
					Thread thread = new Thread(start);
					thread.Name = i.ToString();

					// add the thread to the thread pool
					_threadPool[i] = thread;

					// start the thread for the current URI
					thread.Start(uri);
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Processes the handler.
		/// </summary>
		/// <param name="element">The element.</param>
		private void ProcessHandler(object element)
		{
			if (element is UriElement == false)
				return;

			UriElement uri = element as UriElement;

			try
			{
				HttpWebRequest request = WebRequest.CreateDefault(uri) as HttpWebRequest;

				// if the request cannot be established for the current URI then continue
				if (request == null)
					return;

				request.Method = RequestMethod;
				request.UserAgent = UserAgent;
				request.AllowAutoRedirect = false;

				HttpWebResponse response;
				try
				{
					response = request.GetResponse() as HttpWebResponse;
				}
				catch (WebException exc)
				{
					response = exc.Response as HttpWebResponse;
				}

				// if the response cannot be established for the current request then continue
				if (response == null)
					return;

				using (UriHandler handler = new UriHandler(uri, response))
				{
					handler.Started += new EventHandler<UriHandlerStartedEventArgs>(handler_Started);
					handler.UriFound += new EventHandler<UriFoundEventArgs>(handler_UriFound);
					handler.Finished += new EventHandler<UriProcessingFinishedEventArgs>(handler_Finished);
					handler.Process();
				}

				if (response != null)
					response.Close();
			}
			catch (Exception)
			{
				OnUriError(new UriFoundEventArgs(new UriElement(InitialUrl, uri)));
			}
		}

		/// <summary>
		/// Determines whether [is processing required] [the specified URI].
		/// </summary>
		/// <param name="uri">The URI.</param>
		/// <returns>
		/// 	<see langword="true"/> if [is processing required] [the specified URI]; otherwise, <see langword="false"/>.
		/// </returns>
		private bool IsProcessingRequired(UriElement uri)
		{
			return !_processing.ContainsKey(uri) && !_processed.Contains(uri) && !_notProcessed.Contains(uri) && (InitialUrl.IsBaseOf(uri) || uri.RequestedUri.Host.Contains(InitialUrl.Host) || InitialUrl.Host.Contains(uri.RequestedUri.Host));
		}

		/// <summary>
		/// Determines whether the specified URI is processed.
		/// </summary>
		/// <param name="uri">The URI.</param>
		/// <returns>
		/// 	<see langword="true"/> if the specified URI is processed; otherwise, <see langword="false"/>.
		/// </returns>
		private bool IsProcessed(UriElement uri)
		{
			return _processing.ContainsKey(uri) || _processed.Contains(uri);
		}

		/// <summary>
		/// Handles the Started event of the handler control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void handler_Started(object sender, UriHandlerStartedEventArgs e)
		{
			lock (_sync)
			{
				bool isProcessed = IsProcessed(e.Element);
				e.Cancel = isProcessed;

				if (!isProcessed)
					_processing.Add(e.Element, DateTime.Now);
			}
		}

		/// <summary>
		/// Handles the Finished event of the handler control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="ManagedFusion.Crawler.UriProcessFinishedEventArgs"/> instance containing the event data.</param>
		private void handler_Finished(object sender, UriProcessingFinishedEventArgs e)
		{
			lock (_sync)
			{
				_processing.Remove(e.Element);

				if (!_processed.Contains(e.Element))
				{
					_processed.Add(e.Element);

					// notify of uri finished processing
					OnUriProcessingFinished(e);
				}
			}
		}

		/// <summary>
		/// Handles the UriFound event of the handler control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="ManagedFusion.Crawler.UriFoundEventArgs"/> instance containing the event data.</param>
		private void handler_UriFound(object sender, UriFoundEventArgs e)
		{
			lock (_sync)
			{
				// make sure the URI hasn't been processed, is processing, or in queued up
				if (IsProcessingRequired(e.Element))
				{
					_notProcessed.Enqueue(e.Element);

					OnUriFound(new UriFoundEventArgs(e.Element));
				}
			}
		}
	}
}
