using System.Text.Json.Serialization;

namespace GitLabSymbolServerProxy;

public class GitLabPackage {
	[JsonPropertyName("id")]
	public int Id { get; set; }
	[JsonPropertyName("project_id")]
	public int ProjectId { get; set; }
	[JsonPropertyName("name")]
	public string? Name { get; set; }
	[JsonPropertyName("version")]
	public string? Version { get; set; }
}