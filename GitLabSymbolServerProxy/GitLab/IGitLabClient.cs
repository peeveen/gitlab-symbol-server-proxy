namespace GitLabSymbolServerProxy;

// Interface for our GitLab client. Only need to do one thing: get code!
public interface IGitLabClient {
	public Task<IEnumerable<ISnupkgDescriptor>> GetSnupkgsByName(string name);
	public Task<SnupkgStream> GetSnupkgStream(GitLabSnupkgDescriptor snupkg);
}