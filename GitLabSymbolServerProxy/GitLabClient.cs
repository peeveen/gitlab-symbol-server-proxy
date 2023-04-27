using System.Text;
using LanguageExt;
using static LanguageExt.Prelude;

namespace GitLabSymbolServerProxy;

public class GitLabClient : IGitLabClient {
	/// <summary>
	/// Header in which we pass the access token.
	/// </summary>
	public const string AccessTokenHeader = "PRIVATE-TOKEN";
	/// <summary>
	/// Basic authentication header name.
	/// </summary>
	public const string BasicAuthenticationHeader = "Authorization";

	private readonly HttpClient _httpClient;
	private readonly ILogger _logger;
	private readonly IProxyConfig _config;

	public GitLabClient(IProxyConfig config, ILoggerFactory loggerFactory, HttpClient httpClient) {
		_httpClient = httpClient;
		_logger = loggerFactory.CreateLogger<GitLabClient>();
		_config = config;
	}

	public async Task<Try<IEnumerable<GitLabSymbolPackage>>> GetPackageFilesFromPackages(int packageId, string packageName, string packageVersion, int projectId) {
		var getGroupPackagesUri = new Uri(new Uri(_config.GitLabHostOrigin), $"api/v4/projects/{projectId}/packages/{packageId}/package_files");
		var packageFiles = await ExecutePaginatedRequest<GitLabPackageFile>(getGroupPackagesUri, _config.PersonalAccessToken);
		return packageFiles.Map(fs => fs.Map(f => new GitLabSymbolPackage(projectId, packageId, f.Id, packageName, packageVersion, f.Filename!)).Filter(f => f.Filename.EndsWith(".snupkg")));
	}

	public async Task<Try<IEnumerable<GitLabSymbolPackage>>> GetSnupkgFilesFromGroup(string name, int groupId) =>
		await TryAsync(async () => {
			var getGroupPackagesUri = new Uri(new Uri(_config.GitLabHostOrigin), $"api/v4/groups/{groupId}/packages?package_type=nuget&status=default&package_name={name.Replace(".pdb", string.Empty, StringComparison.OrdinalIgnoreCase)}");
			var packages = await ExecutePaginatedRequest<GitLabPackage>(getGroupPackagesUri, _config.PersonalAccessToken).IfFailThrow();
			var tryPackageFiles = await packages.Map(pkg => GetPackageFilesFromPackages(pkg.Id, pkg.Name!, pkg.Version!, pkg.ProjectId)).TraverseParallel(x => x);
			var packageFiles = tryPackageFiles.Aggregate().IfFailThrow().SelectMany(x => x);
			return packageFiles;
		});

	public async Task<Try<IEnumerable<GitLabSymbolPackage>>> GetSnupkgsByName(string name) =>
		await TryAsync(async () => {
			// Get all top-level groups.
			var getTopLevelGroupsRequestUri = new Uri(new Uri(_config.GitLabHostOrigin), "api/v4/groups?top_level_only=true");
			var groups = await ExecutePaginatedRequest<GitLabGroup>(getTopLevelGroupsRequestUri, _config.PersonalAccessToken).IfFailThrow();
			var tryPackageFiles = await groups.Map(group => GetSnupkgFilesFromGroup(name, group.Id)).TraverseParallel(x => x);
			var packageFiles = tryPackageFiles.Aggregate().IfFailThrow().SelectMany(x => x);
			return packageFiles;
		});

	public async Task<Stream> GetSnupkgStream(string projectId, string packageName, string packageVersion, string packageFilename) {
		var downloadNuGetPackageUri = new Uri(new Uri(_config.GitLabHostOrigin), $"api/v4/projects/{projectId}/packages/nuget/download/{packageName}/{packageVersion}/{packageFilename}");
		var response = await MakeApiRequest(HttpMethod.Get, downloadNuGetPackageUri, _config.PersonalAccessToken, _config.UserName);
		return await response.Content.ReadAsStreamAsync();
	}

	private static string Base64Encode(string str) => Convert.ToBase64String(Encoding.UTF8.GetBytes(str));

	private static string CreateBasicAuthenticationHeader(string userName, string password) =>
		$"Basic {Base64Encode($"{userName}:{password}")}";

	private async Task<HttpResponseMessage> MakeApiRequest(HttpMethod method, Uri uri, string accessToken, string? userName = null) {
		using var req = new HttpRequestMessage(method, uri);
		if (string.IsNullOrEmpty(userName))
			req.Headers.Add(AccessTokenHeader, accessToken);
		else
			req.Headers.Add(BasicAuthenticationHeader, CreateBasicAuthenticationHeader(userName, accessToken));
		// Ask GitLab for the list of all items
		var res = await _httpClient.SendAsync(req);
		res.EnsureSuccessStatusCode();
		return res;
	}

	// GitLab returns data in paginated form. Rather annoying, all told.
	private async Task<Try<IEnumerable<T>>> ExecutePaginatedRequest<T>(Uri uri, string accessToken) =>
		await TryAsync(async () => {
			IEnumerable<T> items = List<T>();
			var nextUri = uri;
			while (nextUri != null) {
				// Ask GitLab for the list of all items
				using var res = await MakeApiRequest(HttpMethod.Get, nextUri, accessToken);
				// Parse the JSON response into an array of objects.
				Option<IEnumerable<T>> pageItems = await res.Content.ReadFromJsonAsync<T[]>();
				items = items.Append(pageItems.Bind(l => l));
				// If there is another page, it'll be a "Next" link in the headers.
				LinkHeaders linkHeaders = new(res);
				nextUri = linkHeaders.NextLink;
			}
			return items;
		});
}
