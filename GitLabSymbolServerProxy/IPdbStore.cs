namespace GitLabSymbolServerProxy;

public interface IPdbStore {
	Task StorePdbs(IEnumerable<PdbStream> pdbs);
	Task<Stream> GetPdb(string name, string hash);
	Task<ICacheManifest> ReadCacheManifest();
	Task StoreCacheManifest(ICacheManifest manifest);
	Task Clear();
}