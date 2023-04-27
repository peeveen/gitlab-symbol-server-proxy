using LanguageExt;

namespace GitLabSymbolServerProxy;

public interface IPdbCache {
	Task StorePdbs(IEnumerable<PdbStream> pdbs);
	bool IsSnupkgKnown(SnupkgStream snupkgStream);
	void RegisterSnupkgs(IEnumerable<SnupkgStream> snupkgs);
	Task<Option<Stream>> GetPdb(string name, string pdbHash);
	void Clear();
}