using System.Text.Json;

namespace GitLabSymbolServerProxy;

public class FileSystemPdbStore : IPdbStore {
	private const string ManifestFilename = "cacheManifest.json";
	private readonly DirectoryInfo _root;
	private readonly FileInfo _cacheManifest;
	private readonly SemaphoreSlim _fileLock = new(1);

	public FileSystemPdbStore(IProxyConfig config) {
		_root = Directory.CreateDirectory(config.CacheRootPath);
		_cacheManifest = new FileInfo(Path.Combine(_root.FullName, ManifestFilename));
	}

	private async Task WithLock(Action action) {
		await _fileLock.WaitAsync();
		try {
			action();
		} finally {
			_fileLock.Release();
		}
	}

	private async Task WithLock(Func<Task> fn) {
		await _fileLock.WaitAsync();
		try {
			await fn();
		} finally {
			_fileLock.Release();
		}
	}

	public async Task Clear() => await WithLock(() => {
		_root.Delete(true);
		_root.Create();
	});

	public async Task<ICacheManifest> ReadCacheManifest() {
		if (_cacheManifest.Exists) {
			using var cacheManifestStream = _cacheManifest.OpenRead();
			return await JsonSerializer.DeserializeAsync<CacheManifest>(cacheManifestStream) ?? new CacheManifest();
		}
		return new CacheManifest();
	}

	public async Task StoreCacheManifest(ICacheManifest manifest) => await WithLock(async () => {
		using var fileStream = _cacheManifest.OpenWrite();
		await JsonSerializer.SerializeAsync(fileStream, manifest);
	});

	private string GetPdbPath(string filename, string hash) => Path.Combine(_root.FullName, filename, hash, filename);

	public async Task StorePdbs(IEnumerable<PdbStream> pdbs) {
		foreach (var pdb in pdbs) {
			var path = GetPdbPath(pdb.Filename, pdb.PdbHash);
			var fileToCreate = new FileInfo(path);
			fileToCreate.Directory?.Create();
			using var outFileStream = fileToCreate.Create();
			await pdb.Stream.CopyToAsync(outFileStream);
		}
	}

	Task<Stream> IPdbStore.GetPdb(string name, string hash) => Task.Run(() => {
		var path = GetPdbPath(name, hash);
		var file = new FileInfo(path);
		return file.OpenRead() as Stream;
	});
}


/*
				if (!IsPdbKnown(name, pdbHash, out var hashMaps)) {
					// If not, better download it.
				}
*/