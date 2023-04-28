namespace GitLabSymbolServerProxy;

public interface ISnupkgDescriptor {
	string PackageName { get; }
	string Filename { get; }
	string Version { get; }
}