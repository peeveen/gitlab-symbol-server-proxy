using LanguageExt;

namespace GitLabSymbolServerProxy;

public interface IPdbCache {
	Task AddPdbFromSnupkgContent(string name, Stream snupkgContent);
	Option<IList<string>> GetPdbHashes(string name, string snupkgHash);
	Option<string> GetPdbPath(string name, string pdbHash);
	void Clear();
}