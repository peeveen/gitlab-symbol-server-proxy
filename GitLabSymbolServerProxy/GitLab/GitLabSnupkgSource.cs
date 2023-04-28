using LanguageExt;

namespace GitLabSymbolServerProxy;

public class GitLabSnupkgSource : ISnupkgSource {
	private readonly IGitLabClient _gitLabClient;
	private readonly ILogger _logger;

	public GitLabSnupkgSource(IGitLabClient gitLabClient, ILoggerFactory loggerFactory) {
		_gitLabClient = gitLabClient;
		_logger = loggerFactory.CreateLogger<GitLabSnupkgSource>();
	}

	public async Task<IEnumerable<ISnupkgDescriptor>> GetSnupkgs(string name) {
		var snupkgs = (await _gitLabClient.GetSnupkgsByName(name)).ToList();
		_logger.LogDebug("Found {PackageFileCount} snupkg files in packages that matched the name {Name} ...", snupkgs.Count, name);
		return snupkgs;
	}

	public async Task<IEnumerable<SnupkgStream>> GetSnupkgStreams(IEnumerable<ISnupkgDescriptor> snupkgs) {
		var gitLabSnupkgs = snupkgs.Cast<GitLabSnupkgDescriptor>();
		return await gitLabSnupkgs.Map(async pkg => {
			_logger.LogDebug("Getting {Filename} stream from {PackageName} {PackageVersion} ...", pkg.Filename, pkg.PackageName, pkg.Version);
			return await _gitLabClient.GetSnupkgStream(pkg);
		}).TraverseParallel(x => x);
	}
}
