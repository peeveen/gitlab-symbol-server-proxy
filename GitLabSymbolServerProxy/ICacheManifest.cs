namespace GitLabSymbolServerProxy;

public interface ICacheManifest {
	void Clear();
	bool HasSnupkg(ISnupkgDescriptor snupkg);
	void AddSnupkgs(IEnumerable<ISnupkgDescriptor> snupkgs);
}