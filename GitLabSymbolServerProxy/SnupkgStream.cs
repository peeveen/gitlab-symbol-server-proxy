using System.IO.Compression;
using System.Security.Cryptography;
using LanguageExt;

namespace GitLabSymbolServerProxy;

public sealed class SnupkgStream : IDisposable {
	public SnupkgStream(Stream stream, string filename, string packageName, string version) {
		Stream = stream;
		Filename = filename;
		PackageName = packageName;
		Version = version;
		// To allow seeking, need to copy this into memory.
		// No Content-Length header is provided, so this is a bit sub-optimal.
		SnupkgContent = new MemoryStream();
		Stream.CopyTo(SnupkgContent);
		// Create a hash by which we will identify this snupkg file in the future.
		using var sha256 = SHA256.Create();
		var sha256Hash = sha256.ComputeHash(SnupkgContent);
		SnupkgHash = Convert.ToBase64String(sha256Hash);
	}

	public async Task<IEnumerable<PdbStream>> GetPdbs() {
		// Reset stream for zip reading.
		SnupkgContent.Seek(0, SeekOrigin.Begin);
		// Open the snupkg (it's a zip file, basically)
		using var zip = new ZipArchive(SnupkgContent);
		// Look for any PDB files.
		var pdbEntries = zip.Entries.Filter(entry => entry.Name.EndsWith(".pdb"));
		return await pdbEntries.Map(pdbEntry => Task.Run(async () => {
			// Again, need to read the PDB into memory for seeking purposes.
			var pdbByteStream = new MemoryStream((int)pdbEntry.Length);
			using var entryStream = pdbEntry.Open();
			await entryStream.CopyToAsync(pdbByteStream);
			// Get the PDB hash value (this is the internal GUID represented as raw
			// hex, plus, for some reason, a load of f's)
			pdbByteStream.Seek(0, SeekOrigin.Begin);
			return new PdbStream(pdbByteStream, pdbEntry.Name, this);
		})).TraverseParallel(x => x);
	}

	public void Dispose() {
		Stream.Dispose();
		GC.SuppressFinalize(this);
	}

	public Stream Stream { get; }
	public string Filename { get; }
	public string PackageName { get; }
	public string Version { get; }
	public string SnupkgHash { get; }

	private MemoryStream SnupkgContent { get; }
}