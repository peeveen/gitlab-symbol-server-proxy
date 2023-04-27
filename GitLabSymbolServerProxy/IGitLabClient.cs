using LanguageExt;

namespace GitLabSymbolServerProxy;

// Interface for our GitLab client. Only need to do one thing: get code!
public interface IGitLabClient {
	public Task<IEnumerable<GitLabSymbolPackage>> GetSnupkgsByName(string name);
	public Task<SnupkgStream> GetSnupkgStream(string projectId, string packageName, string packageVersion, string packageFilename);
}