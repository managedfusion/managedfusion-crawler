using System;
using System.Collections.Generic;
using System.Net;

namespace ManagedFusion.Crawler
{
	/// <summary>
	/// 
	/// </summary>
	public class UriProcessingFinishedEventArgs : EventArgs
	{
		private string _method;
		private int _status;
		private UriElement _element;
		private UriElement[] _related;
		private string _contentHash;
		private string _content;
		private WebHeaderCollection _responseHeaders;
		private TimeSpan _responseTime;

		/// <summary>
		/// Initializes a new instance of the <see cref="UriProcessFinishedEventArgs"/> class.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="response">The response.</param>
		/// <param name="related">The related.</param>
		/// <param name="contentHash">The content hash.</param>
		/// <param name="content">The content.</param>
		public UriProcessingFinishedEventArgs(UriElement element, HttpWebResponse response, UriElement[] related, string contentHash, string content, TimeSpan responseTime)
		{
			_element = element;
			_method = response.Method;
			_status = (int)response.StatusCode;
			_responseHeaders = response.Headers;
			_contentHash = contentHash;
			_content = content;
			_related = related;
			_responseTime = responseTime;
		}

		/// <summary>
		/// Gets the method.
		/// </summary>
		/// <value>The method.</value>
		public string Method
		{
			get { return _method; }
		}

		/// <summary>
		/// Gets the status.
		/// </summary>
		/// <value>The status.</value>
		public int Status
		{
			get { return _status; }
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
		/// Gets the related.
		/// </summary>
		/// <value>The related.</value>
		public UriElement[] Related
		{
			get { return _related; }
		}

		/// <summary>
		/// Gets the hash.
		/// </summary>
		/// <value>The hash.</value>
		public string ContentHash
		{
			get { return _contentHash; }
		}

		/// <summary>
		/// Gets the content.
		/// </summary>
		/// <value>The content.</value>
		public string Content
		{
			get { return _content; }
		}

		/// <summary>
		/// Gets the response headers.
		/// </summary>
		/// <value>The response headers.</value>
		public WebHeaderCollection ResponseHeaders
		{
			get { return _responseHeaders; }
		}

		/// <summary>
		/// Gets the response time.
		/// </summary>
		/// <value>The response time.</value>
		public TimeSpan ResponseTime
		{
			get { return _responseTime; }
		}
	}
}