using System;
using System.Collections.Generic;
using System.Text;

namespace ManagedFusion.Crawler
{
	/// <summary>
	/// 
	/// </summary>
	public class UriElement : IComparable<UriElement>, IComparable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UriElement"/> class.
		/// </summary>
		/// <param name="cameFrom">The came from.</param>
		/// <param name="foundUri">The found URI.</param>
		public UriElement(Uri cameFrom, Uri foundUri)
		{
			if (cameFrom == null)
				throw new ArgumentNullException("cameFrom");

			if (foundUri == null)
				throw new ArgumentNullException("foundUri");

			CameFromUri = cameFrom;
			FoundUri = foundUri;
			ActualUri = new Uri(CameFromUri, FoundUri);
			RequestedUri = new Uri(ActualUri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped));

			string absolutePath = ActualUri.GetLeftPart(UriPartial.Path);
			int endingSlashIndex = absolutePath.LastIndexOf("/");

			if (endingSlashIndex != -1)
				BaseUri = new Uri(absolutePath.Substring(0, endingSlashIndex + 1));
			else
				BaseUri = new Uri(absolutePath);
		}

		/// <summary>
		/// Gets the came from.
		/// </summary>
		/// <value>The came from.</value>
		public Uri CameFromUri
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the found URI.
		/// </summary>
		/// <value>The found URI.</value>
		public Uri FoundUri
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the requested URI.
		/// </summary>
		/// <value>The requested URI.</value>
		public Uri RequestedUri
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the actual URI.
		/// </summary>
		/// <value>The actual URI.</value>
		public Uri ActualUri
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the base URI.
		/// </summary>
		/// <value>The base URI.</value>
		public Uri BaseUri
		{
			get;
			private set;
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="ManagedFusion.Crawler.UriElement"/> to <see cref="System.Uri"/>.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns>The result of the conversion.</returns>
		public static implicit operator Uri(UriElement element)
		{
			return element.RequestedUri;
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>.
		/// </summary>
		/// <param name="obj">The <see cref="T:System.Object"></see> to compare with the current <see cref="T:System.Object"></see>.</param>
		/// <returns>
		/// true if the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>; otherwise, false.
		/// </returns>
		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			if (!(obj is UriElement))
				return false;

			return CompareTo(obj as UriElement) == 0;
		}

		/// <summary>
		/// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"></see>.
		/// </returns>
		public override int GetHashCode()
		{
			return RequestedUri.GetHashCode();
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </returns>
		public override string ToString()
		{
			return RequestedUri.ToString();
		}

		#region IComparable<UriElement> Members

		/// <summary>
		/// Compares the current object with another object of the same type.
		/// </summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>
		/// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the other parameter.Zero This object is equal to other. Greater than zero This object is greater than other.
		/// </returns>
		public int CompareTo(UriElement other)
		{
			return Uri.Compare(RequestedUri, other.RequestedUri, UriComponents.HttpRequestUrl, UriFormat.UriEscaped, StringComparison.InvariantCultureIgnoreCase);
		}

		#endregion

		#region IComparable Members

		/// <summary>
		/// Compares the current instance with another object of the same type.
		/// </summary>
		/// <param name="obj">An object to compare with this instance.</param>
		/// <returns>
		/// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance is less than obj. Zero This instance is equal to obj. Greater than zero This instance is greater than obj.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">obj is not the same type as this instance. </exception>
		int IComparable.CompareTo(object obj)
		{
			if (obj == null)
				return 1;

			if (!(obj is UriElement))
				return 1;

			return CompareTo(obj as UriElement);
		}

		#endregion
	}
}
