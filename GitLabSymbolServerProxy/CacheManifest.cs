using StringHashSet = System.Collections.Generic.HashSet<string>;

namespace GitLabSymbolServerProxy;

public class CacheManifest : ICacheManifest {
	// Language-Ext has better collections, but this is more easily JSON-serializable.
	public StringHashSet KnownSnupkgs { get; set; } = new StringHashSet();
	private readonly object _cacheLock = new();

	private static string MakeSnupkgKey(ISnupkgDescriptor snupkg) => $"{snupkg.PackageName},{snupkg.Version}";

	public void AddSnupkgs(IEnumerable<ISnupkgDescriptor> snupkgs) {
		lock (_cacheLock) {
			foreach (var snupkg in snupkgs)
				KnownSnupkgs.Add(MakeSnupkgKey(snupkg));
		}
	}

	public void Clear() {
		lock (_cacheLock) {
			KnownSnupkgs = new StringHashSet();
		}
	}

	public bool HasSnupkg(ISnupkgDescriptor snupkg) {
		lock (_cacheLock) {
			return KnownSnupkgs.Contains(MakeSnupkgKey(snupkg));
		}
	}
}