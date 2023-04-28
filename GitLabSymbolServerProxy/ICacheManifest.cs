namespace GitLabSymbolServerProxy;

public interface ICacheManifest {
	void Clear();
	bool HasSnupkg(ISnupkgDescriptor snupkg);
	void AddPdbs(IEnumerable<PdbStream> pdbs);
	void AddSnupkgs(IEnumerable<ISnupkgDescriptor> snupkgs);
}