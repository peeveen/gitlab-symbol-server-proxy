using LanguageExt;
using static LanguageExt.Prelude;

namespace GitLabSymbolServerProxy;

public class SymbolCache : ISymbolCache {
	private readonly ISymbolStore _pdbStore;
	private readonly AtomHashMap<string, Unit> _knownSnupkgs = AtomHashMap<string, Unit>();

	private static string MakeSnupkgKey(ISnupkgDescriptor snupkg) =>
		$"{snupkg.PackageName},{snupkg.Version}";

	public SymbolCache(ISymbolStore pdbStore) {
		_pdbStore = pdbStore;
		AddKnownSnupkgIds(pdbStore.GetKnownSnupkgIds().Result);
	}

	public async Task Clear() {
		_knownSnupkgs.Clear();
		await StoreKnownSnupkgIds();
	}

	public async Task AddSnupkgs(IEnumerable<ISnupkgDescriptor> snupkgs) {
		AddKnownSnupkgIds(snupkgs.Map(pkg => MakeSnupkgKey(pkg)));
		await StoreKnownSnupkgIds();
	}

	public bool IsSnupkgKnown(ISnupkgDescriptor snupkg) =>
		_knownSnupkgs.ContainsKey(MakeSnupkgKey(snupkg));

	private async Task StoreKnownSnupkgIds() => await _pdbStore.StoreKnownSnupkgIds(_knownSnupkgs.Keys);
	private void AddKnownSnupkgIds(IEnumerable<string> knownSnupkgIds) => _knownSnupkgs.AddRange(knownSnupkgIds.Map(id => (id, Unit.Default)));
}
