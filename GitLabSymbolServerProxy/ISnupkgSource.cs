namespace GitLabSymbolServerProxy;

public interface ISnupkgSource {
	Task<IEnumerable<ISnupkgDescriptor>> GetSnupkgs(string name);
	Task<IEnumerable<SnupkgStream>> GetSnupkgStreams(IEnumerable<ISnupkgDescriptor> snupkgs);
}