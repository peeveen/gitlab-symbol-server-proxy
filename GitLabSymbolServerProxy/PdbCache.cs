using LanguageExt;

namespace GitLabSymbolServerProxy;

public class PdbCache : IPdbCache {
	private readonly IPdbStore _pdbStore;
	private readonly ICacheManifest _cacheManifest;

	public PdbCache(IPdbStore pdbStore) {
		_pdbStore = pdbStore;
		_cacheManifest = pdbStore.ReadCacheManifest().Result;
	}

	public async Task<Option<Stream>> GetPdb(string name, string pdbHash) => await _pdbStore.GetPdb(name, pdbHash);

	public void Clear() {
		_cacheManifest.Clear();
		_pdbStore.Clear();
	}

	public async Task StorePdbs(IEnumerable<PdbStream> pdbs) {
		await _pdbStore.StorePdbs(pdbs);
		_cacheManifest.AddPdbs(pdbs);
		await _pdbStore.StoreCacheManifest(_cacheManifest);
	}

	public bool IsSnupkgKnown(ISnupkgDescriptor snupkg) => _cacheManifest.HasSnupkg(snupkg);

	public void RegisterSnupkgs(IEnumerable<ISnupkgDescriptor> snupkgs) => _cacheManifest.AddSnupkgs(snupkgs);
}
