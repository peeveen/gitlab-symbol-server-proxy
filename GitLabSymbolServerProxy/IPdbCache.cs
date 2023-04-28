using LanguageExt;

namespace GitLabSymbolServerProxy;

public interface IPdbCache {
	Task StorePdbs(IEnumerable<PdbStream> pdbs);
	bool IsSnupkgKnown(ISnupkgDescriptor snupkg);
	void RegisterSnupkgs(IEnumerable<ISnupkgDescriptor> snupkgs);
	Task<Option<Stream>> GetPdb(string name, string pdbHash);
	void Clear();
}