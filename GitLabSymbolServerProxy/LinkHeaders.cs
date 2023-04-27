using System.Globalization;
using System.Text.RegularExpressions;

namespace GitLabSymbolServerProxy;

/// <summary>
/// Class largely copied from interweb for parsing Link headers from HTTP responses.
/// </summary>
public class LinkHeaders {
	/// <summary>
	/// The link of type "First", if found.
	/// </summary>
	public Uri? FirstLink { get; }
	/// <summary>
	/// The link of type "Prev", if found.
	/// </summary>
	public Uri? PrevLink { get; }
	/// <summary>
	/// The link of type "Next", if found.
	/// </summary>
	public Uri? NextLink { get; }
	/// <summary>
	/// The link of type "Last", if found.
	/// </summary>
	public Uri? LastLink { get; }

	/// <summary>
	/// Constructor. Parses the link headers from the given response message.
	/// </summary>
	/// <param name="httpResponseMessage">Response message.</param>
	/// <returns>Link headers found.</returns>
	public LinkHeaders(HttpResponseMessage httpResponseMessage) : this(httpResponseMessage.Headers.GetValues("Link").FirstOrDefault()) { }

	/// <summary>
	/// Constructor. Parses the link headers from the given header string.
	/// </summary>
	/// <param name="linkHeaderStr">Header string.</param>
	/// <returns>Link headers found.</returns>
	public LinkHeaders(string? linkHeaderStr) {
		static Uri? ToUri(string? linkString) => linkString == null ? null : new Uri(linkString);
		if (!string.IsNullOrWhiteSpace(linkHeaderStr)) {
			string[] linkStrings = linkHeaderStr.Split(',');

			if (linkStrings != null && linkStrings.Any()) {
				foreach (string linkString in linkStrings) {
					var relMatch = Regex.Match(linkString, "(?<=rel=\").+?(?=\")", RegexOptions.IgnoreCase);
					var linkMatch = Regex.Match(linkString, "(?<=<).+?(?=>)", RegexOptions.IgnoreCase);

					if (relMatch.Success && linkMatch.Success) {
						string rel = relMatch.Value.ToUpper(CultureInfo.InvariantCulture);
						string link = linkMatch.Value;

						switch (rel) {
							case "FIRST":
								FirstLink = ToUri(link);
								break;
							case "PREV":
								PrevLink = ToUri(link);
								break;
							case "NEXT":
								NextLink = ToUri(link);
								break;
							case "LAST":
								LastLink = ToUri(link);
								break;
						}
					}
				}
			}
		}
	}
}
