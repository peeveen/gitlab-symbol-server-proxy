using System.Text.Json.Serialization;

namespace GitLabSymbolServerProxy;

public class GitLabPackageFile {
	[JsonPropertyName("id")]
	public int Id { get; set; }
	[JsonPropertyName("package_id")]
	public int PackageId { get; set; }
	[JsonPropertyName("file_name")]
	public string? Filename { get; set; }
	[JsonPropertyName("file_md5")]
	public string? FileMd5 { get; set; }
	[JsonPropertyName("file_sha1")]
	public string? FileSha1 { get; set; }
	[JsonPropertyName("file_sha256")]
	public string? FileSha256 { get; set; }
}