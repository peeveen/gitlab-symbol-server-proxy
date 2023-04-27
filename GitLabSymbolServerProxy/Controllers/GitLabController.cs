using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;
using LanguageExt;
using Microsoft.AspNetCore.Mvc;

namespace GitLabSymbolServerProxy.Controllers;

[Controller]
[Route("/")]
public class GitLabController : Controller {
	private readonly IGitLabClient _gitLabClient;
	private readonly ILogger _logger;

	public GitLabController(ILoggerFactory loggerFactory, IGitLabClient gitLabClient) {
		_gitLabClient = gitLabClient;
		_logger = loggerFactory.CreateLogger<GitLabController>();
	}

	[HttpGet]
	[Route("/version")]
	[SuppressMessage("csharp", "CA1822")]
	public string GetVersion() => Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";

	[HttpGet]
	[Route("/index2.txt")]
	// We are only operating on a two-tier basis.
	public ActionResult GetThreeTierIndexFile() => NotFound();

	[HttpGet]
	[Route("/{filename}/{hash}/{filename2}")]
	public async Task<ActionResult> GetSymbols([FromServices] IPdbCache pdbCache, [FromServices] IProxyConfig config, [FromRoute] string filename, [FromRoute] string hash, [FromRoute] string filename2) {
		// We only return PDBs.
		if (filename != filename2 || !filename.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase) || !filename2.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase))
			return NotFound();
		// Check regexs.
		if (config.SupportedPdbNames.Any() && !config.SupportedPdbNames.Any(regex => new Regex(regex).IsMatch(filename)))
			return NotFound();
		var filenameWithoutExtension = filename.Replace(".pdb", string.Empty, StringComparison.OrdinalIgnoreCase);
		// We need to find the snupkg that matches the name in the pdbRequest.
		// We might already have it!
		var pdbPath = pdbCache.GetPdbPath(filename, hash);
		if (pdbPath.IsNone) {
			_logger.LogInformation("We don't already have {Filename}, so looking in GitLab ...", filename);
			var snupkgFilesByName = (await _gitLabClient.GetSnupkgsByName(filename).IfFailThrow()).ToList();
			_logger.LogDebug("Found {PackageFileCount} snupkg files in packages that matched the name {Name}", snupkgFilesByName.Count, filenameWithoutExtension);
			var fetchTasks = snupkgFilesByName.Map(async pkg => {
				_logger.LogDebug("Downloading {Filename} from {PackageName} {PackageVersion} ...", pkg.Filename, pkg.PackageName, pkg.Version);
				await pdbCache.AddPdbFromSnupkgContent(
					filename,
					await _gitLabClient.GetSnupkgStream(pkg.ProjectId.ToString(), filenameWithoutExtension, pkg.Version, pkg.Filename)
				);
			}).ToArray();
			await Task.WhenAll(fetchTasks);
			pdbPath = pdbCache.GetPdbPath(filename, hash);
		}
		return pdbPath.Match<ActionResult>(
			Some: path => {
				_logger.LogInformation("Returning {Path} in response stream.", path);
				return new FileStreamResult(new FileInfo(path).OpenRead(), "application/octet-stream");
			},
			None: () => NotFound()
		);
	}
}
