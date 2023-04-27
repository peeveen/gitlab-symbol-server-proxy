using System.Text.Json.Serialization;

namespace GitLabSymbolServerProxy;

public class GitLabGroup {
	[JsonPropertyName("id")]
	public int Id { get; set; }
}