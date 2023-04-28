using LanguageExt;

namespace GitLabSymbolServerProxy;

public interface ISymbolStore {
	Task StorePdbs(IEnumerable<PdbStream> pdbs);
	Task<Option<PdbStream>> GetPdbStream(string name, string hash);
	Task Clear();

	Task<IEnumerable<string>> GetKnownSnupkgIds();
	Task StoreKnownSnupkgIds(IEnumerable<string> snupkgIds);
}