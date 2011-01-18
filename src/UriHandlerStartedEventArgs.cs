using System;
using System.Collections.Generic;
using System.Text;

namespace ManagedFusion.Crawler
{
	/// <summary>
	/// 
	/// </summary>
	public class UriHandlerStartedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UriHandlerStartedEventArgs"/> class.
		/// </summary>
		public UriHandlerStartedEventArgs(UriElement element)
		{
			Element = element;
			Cancel = false;
		}

		/// <summary>
		/// Gets or sets the element.
		/// </summary>
		/// <value>The element.</value>
		public UriElement Element { get; private set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="UriHandlerStartedEventArgs"/> is cancel.
		/// </summary>
		/// <value>
		/// 	<see langword="true"/> if cancel; otherwise, <see langword="false"/>.
		/// </value>
		public bool Cancel { get; set; }
	}
}
