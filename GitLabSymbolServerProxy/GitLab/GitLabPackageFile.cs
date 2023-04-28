using System.Text.Json.Serialization;

namespace GitLabSymbolServerProxy;

public class GitLabPackageFile {
	[JsonPropertyName("id")]
	public int Id { get; set; }
	[JsonPropertyName("package_id")]
	public int PackageId { get; set; }
	[JsonPropertyName("file_name")]
	public string? Filename { get; set; }
}