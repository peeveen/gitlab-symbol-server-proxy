using System.Text.RegularExpressions;

namespace GitLabSymbolServerProxy;

// Interface for our configuration object.
public interface IProxyConfig {
	string GitLabHostOrigin { get; }
	string PersonalAccessToken { get; }
	string UserName { get; }
	string CacheRootPath { get; }
	Regex[] SupportedPdbRegexs { get; }
}