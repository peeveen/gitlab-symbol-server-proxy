using System.Text.RegularExpressions;

namespace GitLabSymbolServerProxy;

// Configuration object, holds the values of arguments supplied on the command line.
public class ProxyConfig : IProxyConfig {
	public const string PersonalAccessTokenArgumentName = "PersonalAccessToken";
	public const string GitLabHostOriginArgumentName = "GitLabHostOrigin";
	public const string CacheRootPathArgumentName = "CacheRootPath";
	public const string UserNameArgumentName = "UserName";
	public const string SupportedPdbNamesArgumentName = "SupportedPdbNames";

	internal ProxyConfig(IConfiguration configuration) {
		static void validateArgument(string name, string value) {
			if (string.IsNullOrEmpty(value))
				throw new Exception($"No value was provided for the configuration property '{name}'.");
		}
		string getArgument(string argName, string defaultValue = "") => (configuration[argName] ?? defaultValue).Trim();

		GitLabHostOrigin = getArgument(GitLabHostOriginArgumentName);
		PersonalAccessToken = getArgument(PersonalAccessTokenArgumentName);
		CacheRootPath = getArgument(CacheRootPathArgumentName);
		UserName = getArgument(UserNameArgumentName);
		SupportedPdbRegexs = configuration.GetSection(SupportedPdbNamesArgumentName).Get<string[]>().Map(s => new Regex(s)).ToArray();
		validateArgument(GitLabHostOriginArgumentName, GitLabHostOrigin);
		validateArgument(PersonalAccessTokenArgumentName, PersonalAccessToken);
		validateArgument(CacheRootPathArgumentName, CacheRootPath);
		validateArgument(UserNameArgumentName, UserName);
	}

	public string GitLabHostOrigin { get; }
	public string PersonalAccessToken { get; }
	public string CacheRootPath { get; }
	public string UserName { get; }
	public Regex[] SupportedPdbRegexs { get; }
}