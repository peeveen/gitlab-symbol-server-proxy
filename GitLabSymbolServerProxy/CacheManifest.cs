using StringHashSet = System.Collections.Generic.HashSet<string>;

namespace GitLabSymbolServerProxy;

public class CacheManifest : ICacheManifest {
	// Language-Ext has better collections, but these are more easily JSON-serializable.
	public StringHashSet KnownSnupkgs { get; set; } = new StringHashSet();
	public StringHashSet KnownPdbHashes { get; set; } = new StringHashSet();
	private readonly object _cacheLock = new();

	private static string MakePdbKey(string name, string hash) => $"{name}/{hash}";
	private static string MakeSnupkgKey(ISnupkgDescriptor snupkg) => $"{snupkg.PackageName},{snupkg.Version}";

	public void AddPdbs(IEnumerable<PdbStream> pdbs) {
		lock (_cacheLock) {
			foreach (var pdb in pdbs)
				KnownPdbHashes.Add(MakePdbKey(pdb.Filename, pdb.PdbHash));
		}
	}

	public void AddSnupkgs(IEnumerable<ISnupkgDescriptor> snupkgs) {
		lock (_cacheLock) {
			foreach (var snupkg in snupkgs)
				KnownSnupkgs.Add(MakeSnupkgKey(snupkg));
		}
	}

	public void Clear() {
		lock (_cacheLock) {
			KnownPdbHashes = new StringHashSet();
			KnownSnupkgs = new StringHashSet();
		}
	}

	public bool HasSnupkg(ISnupkgDescriptor snupkg) {
		lock (_cacheLock) {
			return KnownSnupkgs.Contains(MakeSnupkgKey(snupkg));
		}
	}
}