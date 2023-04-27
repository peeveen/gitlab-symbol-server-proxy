using LanguageExt;

namespace GitLabSymbolServerProxy;

// Interface for our GitLab client. Only need to do one thing: get code!
public interface IGitLabClient {
	public Task<Try<IEnumerable<GitLabSymbolPackage>>> GetSnupkgsByName(string name);
	public Task<Stream> GetSnupkgStream(string projectId, string packageName, string packageVersion, string packageFilename);
}