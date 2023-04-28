using LanguageExt;

namespace GitLabSymbolServerProxy;

public class FileSystemSymbolStore : ISymbolStore {
	private const string ManifestFilename = "cacheManifest.txt";
	private readonly DirectoryInfo _root;
	private readonly FileInfo _cacheManifest;
	private readonly SemaphoreSlim _fileLock = new(1);

	public FileSystemSymbolStore(IProxyConfig config) {
		_root = Directory.CreateDirectory(config.CacheRootPath);
		_cacheManifest = new FileInfo(Path.Combine(_root.FullName, ManifestFilename));
	}

	public async Task Clear() => await _fileLock.WithLock(() => {
		_root.Delete(true);
		_root.Create();
	});

	private string GetPdbPath(string filename, string hash) => Path.Combine(_root.FullName, filename, hash, filename);

	public async Task StorePdbs(IEnumerable<PdbStream> pdbs) {
		foreach (var pdb in pdbs) {
			var path = GetPdbPath(pdb.Filename, pdb.GetPdbHash());
			var fileToCreate = new FileInfo(path);
			fileToCreate.Directory?.Create();
			using var outFileStream = fileToCreate.Create();
			await pdb.Stream.CopyToAsync(outFileStream);
		}
	}

	Task<Option<PdbStream>> ISymbolStore.GetPdbStream(string name, string hash) => Task.Run(() => {
		var path = GetPdbPath(name, hash);
		var file = new FileInfo(path);
		if (file.Exists)
			return new PdbStream(file.OpenRead(), name);
		return Option<PdbStream>.None;
	});

	public async Task<IEnumerable<string>> GetKnownSnupkgIds() =>
		_cacheManifest.Exists ? await File.ReadAllLinesAsync(_cacheManifest.FullName) : Array.Empty<string>();

	public async Task StoreKnownSnupkgIds(IEnumerable<string> snupkgIds) =>
		await File.WriteAllLinesAsync(_cacheManifest.FullName, snupkgIds);
}