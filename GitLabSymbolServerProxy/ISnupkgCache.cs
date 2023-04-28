namespace GitLabSymbolServerProxy;

public interface ISymbolCache {
	bool IsSnupkgKnown(ISnupkgDescriptor snupkg);
	Task AddSnupkgs(IEnumerable<ISnupkgDescriptor> snupkgs);
	Task Clear();
}