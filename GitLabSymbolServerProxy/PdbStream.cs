using System.Reflection.Metadata;

namespace GitLabSymbolServerProxy;

public sealed class PdbStream : IDisposable {
	public PdbStream(Stream stream, string filename, SnupkgStream snupkgSource) {
		Stream = stream;
		Filename = filename;
		SnupkgSource = snupkgSource;
		PdbHash = GetPdbHash(stream);
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

	public void Dispose() {
		Stream.Dispose();
		GC.SuppressFinalize(this);
	}

	public string Filename { get; }
	public Stream Stream { get; }
	public SnupkgStream SnupkgSource { get; }
	public string PdbHash { get; }
}