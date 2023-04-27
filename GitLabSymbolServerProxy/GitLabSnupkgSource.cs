using LanguageExt;

namespace GitLabSymbolServerProxy;

public class GitLabSnupkgSource : ISnupkgSource {
	private readonly IGitLabClient _gitLabClient;
	private readonly ILogger _logger;

	public GitLabSnupkgSource(IGitLabClient gitLabClient, ILoggerFactory loggerFactory) {
		_gitLabClient = gitLabClient;
		_logger = loggerFactory.CreateLogger<GitLabSnupkgSource>();
	}
	public async Task<IEnumerable<SnupkgStream>> GetSnupkgs(string name) {
		var snupkgFilesByName = (await _gitLabClient.GetSnupkgsByName(name)).ToList();
		_logger.LogDebug("Found {PackageFileCount} snupkg files in packages that matched the name {Name} ...", snupkgFilesByName.Count, name);
		return await snupkgFilesByName.Map(async pkg => {
			_logger.LogDebug("Downloading {Filename} from {PackageName} {PackageVersion} ...", pkg.Filename, pkg.PackageName, pkg.Version);
			return await _gitLabClient.GetSnupkgStream(pkg.ProjectId.ToString(), name, pkg.Version, pkg.Filename);
		}).TraverseParallel(x => x);
	}
}
