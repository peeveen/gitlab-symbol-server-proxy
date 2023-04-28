using System.Reflection.Metadata;

namespace GitLabSymbolServerProxy;

public sealed class PdbStream : IDisposable {
	public PdbStream(Stream stream, string filename) {
		Stream = stream;
		Filename = filename;
		PdbHash = GetPdbHash(stream);
	}

	private static string GetPdbHash(Stream stream) {
		stream.Seek(0, SeekOrigin.Begin);
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
	public string PdbHash { get; }
}