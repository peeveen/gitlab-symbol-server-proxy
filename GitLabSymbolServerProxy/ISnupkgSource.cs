namespace GitLabSymbolServerProxy;

public interface ISnupkgSource {
	Task<IEnumerable<SnupkgStream>> GetSnupkgs(string name);
}