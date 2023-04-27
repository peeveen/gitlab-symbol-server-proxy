using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Reflection;
using System.Text.RegularExpressions;
using LanguageExt;
using Microsoft.AspNetCore.Mvc;

namespace GitLabSymbolServerProxy.Controllers;

[Controller]
[Route("/")]
public class SymbolController : Controller {
	private const string PdbExtension = ".pdb";

	private readonly ILogger _logger;

	public SymbolController(ILoggerFactory loggerFactory) {
		_logger = loggerFactory.CreateLogger<SymbolController>();
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
	public async Task<ActionResult> GetSymbols(
		[FromServices] IPdbCache pdbCache,
		[FromServices] IProxyConfig config,
		[FromServices] ISnupkgSource snupkgSource,
		[FromRoute] string filename,
		[FromRoute] string hash,
		[FromRoute] string filename2
	) {
		// We only return PDBs.
		if (filename != filename2 || !filename.EndsWith(PdbExtension, StringComparison.OrdinalIgnoreCase) || !filename2.EndsWith(PdbExtension, StringComparison.OrdinalIgnoreCase)) {
			_logger.LogInformation("Proxy only supports simple PDB requests, ignoring.");
			return NotFound();
		}

		// Check regexs.
		if (config.SupportedPdbNames.Any() && !config.SupportedPdbNames.Any(regex => new Regex(regex).IsMatch(filename))) {
			_logger.LogInformation("Request does not match supported PDB names, ignoring.");
			return NotFound();
		}

		// We need to find the snupkg that matches the name in the pdbRequest.
		// We might already have it!
		var filenameWithoutExtension = filename.Replace(PdbExtension, string.Empty, StringComparison.OrdinalIgnoreCase);
		var pdbStream = await pdbCache.GetPdb(filename, hash);
		if (pdbStream.IsNone) {
			// Oh well, we don't have it ... better go hunting for snupkgs.
			_logger.LogInformation("We don't already have {Filename}, so looking in our snupkg source ...", filename);
			var snupkgStreams = (await snupkgSource.GetSnupkgs(filenameWithoutExtension))
				// If we've already seen this snupkg, no point in processing it.
				.Filter(snupkg => !pdbCache.IsSnupkgKnown(snupkg))
				.ToList();
			try {
				_logger.LogDebug("Found {PackageFileCount} snupkg files in packages that matched the name {Name}, now extracting PDBs ...", snupkgStreams.Count, filenameWithoutExtension);
				// Open up the snupkgs and get all the PDBs that they contain.
				var pdbStreams = (await snupkgStreams.Map(async s => await s.GetPdbs()).TraverseParallel(x => x)).SelectMany(x => x).ToList();
				try {
					_logger.LogDebug("Extracted {PdbCount} PDB files, now storing them in the cache ...", pdbStreams.Count);
					// Store the PDBs from the snupkgs somewhere persistent.
					await pdbCache.StorePdbs(pdbStreams);
					// Now that we've stored the PDBs from the snupkgs, register the snupkg content hashes so that
					// we don't bother processing them again.
					pdbCache.RegisterSnupkgs(snupkgStreams);
				} finally {
					pdbStreams.ForEach(stream => stream.Dispose());
				}
			} finally {
				snupkgStreams.ForEach(stream => stream.Dispose());
			}

			// Requery the cache for the required PDB, cos we might have it now!
			pdbStream = await pdbCache.GetPdb(filename, hash);
		}
		return pdbStream.Match<ActionResult>(
			Some: stream => {
				_logger.LogInformation("Returning PDB content in response stream.");
				return File(stream, MediaTypeNames.Application.Octet);
			},
			None: () => NotFound()
		);
	}
}
