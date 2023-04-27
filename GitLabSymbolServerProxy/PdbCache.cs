using System.IO.Compression;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text.Json;
using LanguageExt;

namespace GitLabSymbolServerProxy;

public class PdbCache : IPdbCache {
	private const string ManifestFilename = "cacheManifest.json";

	private readonly object _cacheLock = new();
	private readonly DirectoryInfo _root;
	private Map<string, PdbCacheHashMaps> _hashMapsByName = new();
	private readonly FileInfo _cacheManifest;

	public PdbCache(string rootPath) {
		_root = Directory.CreateDirectory(rootPath);
		_cacheManifest = new FileInfo(Path.Combine(_root.FullName, ManifestFilename));
		if (_cacheManifest.Exists) {
			using var fileStream = _cacheManifest.OpenRead();
			_hashMapsByName = JsonSerializer.Deserialize<Dictionary<string, PdbCacheHashMaps>>(fileStream).ToMap();
		}
	}

	private static string GetPdbHash(Stream stream) {
		using var pdbReaderProvider = MetadataReaderProvider.FromPortablePdbStream(stream, MetadataStreamOptions.LeaveOpen);
		var reader = pdbReaderProvider.GetMetadataReader();
		if (reader.DebugMetadataHeader != null) {
			var id = new BlobContentId(reader.DebugMetadataHeader.Id);
			return $"{id.Guid.ToString("N").ToUpperInvariant()}ffffffff";
		}
		throw new InvalidOperationException("PDB debug metadata header was null.");
	}

	private bool IsPdbKnown(string name, string pdbHash, out PdbCacheHashMaps hashMaps) {
		lock (_cacheLock) {
			(_hashMapsByName, hashMaps) = _hashMapsByName.FindOrAdd(name, new PdbCacheHashMaps());
			return hashMaps.GetPdbPath(pdbHash).IsSome;
		}
	}

	public async Task AddPdbFromSnupkgContent(string name, Stream snupkgContent) {
		using (snupkgContent) {
			// To allow seeking, need to copy this into memory.
			// No Content-Length header is provided, so this is a bit sub-optimal.
			using var snupkgByteStream = new MemoryStream();
			await snupkgContent.CopyToAsync(snupkgByteStream);
			// Create a hash by which we will identify this snupkg file in the future.
			using var sha256 = SHA256.Create();
			var sha256Hash = await sha256.ComputeHashAsync(snupkgByteStream);
			var sha256HashString = Convert.ToBase64String(sha256Hash);
			// Reset stream.
			snupkgByteStream.Seek(0, SeekOrigin.Begin);
			// Open the snupkg (it's a zip file, basically)
			using var zip = new ZipArchive(snupkgByteStream);
			// Look for any PDB files.
			var pdbEntries = zip.Entries.Filter(entry => entry.Name.EndsWith(".pdb"));
			foreach (var pdbEntry in pdbEntries) {
				// Again, need to read the PDB into memory for seeking purposes.
				using var pdbByteStream = new MemoryStream((int)pdbEntry.Length);
				using var entryStream = pdbEntry.Open();
				await entryStream.CopyToAsync(pdbByteStream);
				// Get the PDB hash value (this is the internal GUID represented as raw
				// hex, plus, for some reason, a load of f's)
				pdbByteStream.Seek(0, SeekOrigin.Begin);
				var pdbHash = GetPdbHash(pdbByteStream);
				pdbByteStream.Seek(0, SeekOrigin.Begin);
				// Do we have this PDB already in our cache?
				if (!IsPdbKnown(name, pdbHash, out var hashMaps)) {
					// If not, better download it.
					var path = Path.Combine(_root.FullName, name, pdbHash, name);
					var fileToCreate = new FileInfo(path);
					fileToCreate.Directory?.Create();
					using var outFileStream = fileToCreate.Create();
					await pdbByteStream.CopyToAsync(outFileStream);
					// Now that it's here, add it to the cache.
					lock (_cacheLock) {
						_hashMapsByName = _hashMapsByName.AddOrUpdate(name, hashMaps.AddPdb(sha256HashString, pdbHash, path));
						using var fileStream = _cacheManifest.OpenWrite();
						JsonSerializer.Serialize(fileStream, _hashMapsByName.ToDictionary());
					}
				}
			}
		}
	}

	public Option<IList<string>> GetPdbHashes(string name, string snupkgHash) {
		lock (_cacheLock) {
			var hashMaps = _hashMapsByName.Find(name);
			return hashMaps.Bind(m => m.GetPdbHashes(snupkgHash));
		}
	}

	public Option<string> GetPdbPath(string name, string pdbHash) {
		lock (_cacheLock) {
			var hashMaps = _hashMapsByName.Find(name);
			return hashMaps.Bind(m => m.GetPdbPath(pdbHash));
		}
	}

	public void Clear() {
		lock (_cacheLock) {
			_root.Delete(true);
			_root.Create();
			_hashMapsByName = new Map<string, PdbCacheHashMaps>();
		}
	}
}

internal class PdbCacheHashMaps {
	// I would have liked to have used more Map/Lst types from LanguageExt here, but
	// they don't serialize to JSON very well.
	public IDictionary<string, IList<string>> PdbHashesBySnupkgHash { get; set; }
	public IDictionary<string, string> PdbPathsByPdbHash { get; set; }

	public PdbCacheHashMaps() : this(new Dictionary<string, IList<string>>(), new Dictionary<string, string>()) { }
	private PdbCacheHashMaps(IDictionary<string, IList<string>> pdbHashesBySnupkgHash, IDictionary<string, string> pdbPathsByPdbHash) {
		PdbHashesBySnupkgHash = pdbHashesBySnupkgHash;
		PdbPathsByPdbHash = pdbPathsByPdbHash;
	}

	internal PdbCacheHashMaps AddPdb(string snupkgHash, string pdbHash, string path) {
		if (!PdbHashesBySnupkgHash.TryGetValue(snupkgHash, out var list)) {
			list = new List<string>();
		}
		list.Add(pdbHash);
		PdbHashesBySnupkgHash[snupkgHash] = list;
		PdbPathsByPdbHash[pdbHash] = path;
		return new PdbCacheHashMaps(PdbHashesBySnupkgHash, PdbPathsByPdbHash);
	}

	internal Option<IList<string>> GetPdbHashes(string snupkgHash) => PdbHashesBySnupkgHash.Find(kvp => kvp.Key == snupkgHash).Map(kvp => kvp.Value);
	internal Option<string> GetPdbPath(string pdbHash) => PdbPathsByPdbHash.Find(kvp => kvp.Key == pdbHash).Map(kvp => kvp.Value);
}
