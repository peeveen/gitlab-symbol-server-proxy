using LanguageExt;

namespace GitLabSymbolServerProxy;

public class CacheManifest : ICacheManifest {
	// Language-Ext has better collections, but these are more easily JSON-serializable.
	public ISet<string> KnownSnupkgHashes { get; set; } = new System.Collections.Generic.HashSet<string>();
	public ISet<string> KnownPdbHashes { get; set; } = new System.Collections.Generic.HashSet<string>();
	private readonly object _cacheLock = new();

	private static string MakePdbKey(string name, string hash) => $"{name}/{hash}";

	public void AddPdbs(IEnumerable<PdbStream> pdbs) {
		lock (_cacheLock) {
			foreach (var pdb in pdbs)
				KnownPdbHashes.Add(MakePdbKey(pdb.Filename, pdb.PdbHash));
		}
	}

	public void AddSnupkgs(IEnumerable<SnupkgStream> snupkgs) {
		lock (_cacheLock) {
			foreach (var snupkg in snupkgs)
				KnownSnupkgHashes.Add(snupkg.SnupkgHash);
		}
	}

	public void Clear() {
		lock (_cacheLock) {
			KnownPdbHashes = new System.Collections.Generic.HashSet<string>();
			KnownSnupkgHashes = new System.Collections.Generic.HashSet<string>();
		}
	}

	public bool HasSnupkg(string hash) {
		lock (_cacheLock) {
			return KnownSnupkgHashes.Contains(hash);
		}
	}
}