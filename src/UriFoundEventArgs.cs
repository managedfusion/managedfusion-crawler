using System;
using System.Collections.Generic;
using System.Text;

namespace ManagedFusion.Crawler
{
	/// <summary>
	/// 
	/// </summary>
	public class UriFoundEventArgs : EventArgs
	{
		private UriElement _element;

		/// <summary>
		/// Initializes a new instance of the <see cref="UriFoundEventArgs"/> class.
		/// </summary>
		/// <param name="cameFrom">The came from.</param>
		/// <param name="foundUri">The found URI.</param>
		public UriFoundEventArgs(Uri cameFrom, Uri foundUri)
		{
			if (cameFrom == null)
				throw new ArgumentNullException("cameFrom");

			if (foundUri == null)
				throw new ArgumentNullException("foundUri");

			_element = new UriElement(cameFrom, foundUri);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UriFoundEventArgs"/> class.
		/// </summary>
		/// <param name="element">The element.</param>
		public UriFoundEventArgs(UriElement element)
		{
			if (element == null)
				throw new ArgumentNullException("element");

			_element = element;
		}

		/// <summary>
		/// Gets the element.
		/// </summary>
		/// <value>The element.</value>
		public UriElement Element
		{
			get { return _element; }
		}
	}
}
