using System.IO.Compression;
using System.Security.Cryptography;
using LanguageExt;

namespace GitLabSymbolServerProxy;

public sealed class SnupkgStream : IDisposable {
	public SnupkgStream(Stream stream) {
		Stream = stream;
	}

	public async Task<IEnumerable<PdbStream>> GetPdbStreams() {
		if (PdbStreams == null) {
			// Open the snupkg (it's a zip file, basically)
			using var zip = new ZipArchive(Stream);
			// Look for any PDB files.
			var pdbEntries = zip.Entries.Filter(entry => entry.Name.EndsWith(".pdb"));
			var pdbStreams = await pdbEntries.Map(pdbEntry => Task.Run(async () => {
				// Need to read the PDB into memory for seeking purposes.
				var pdbByteStream = new MemoryStream((int)pdbEntry.Length);
				using var entryStream = pdbEntry.Open();
				await entryStream.CopyToAsync(pdbByteStream);
				return new PdbStream(pdbByteStream, pdbEntry.Name);
			})).TraverseParallel(x => x);
			PdbStreams = pdbStreams;
		}
		return PdbStreams;
	}

	public void Dispose() {
		Stream.Dispose();
		GC.SuppressFinalize(this);
	}

	public Stream Stream { get; }
	private IEnumerable<PdbStream> PdbStreams { get; set; } = Array.Empty<PdbStream>();
}