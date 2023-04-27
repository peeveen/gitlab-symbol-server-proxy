namespace GitLabSymbolServerProxy;

public interface ICacheManifest {
	void Clear();
	bool HasSnupkg(string hash);
	void AddPdbs(IEnumerable<PdbStream> pdbs);
	void AddSnupkgs(IEnumerable<SnupkgStream> snupkgs);
}