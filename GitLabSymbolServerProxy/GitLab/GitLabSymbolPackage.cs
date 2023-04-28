namespace GitLabSymbolServerProxy;

public class GitLabSymbolPackage {
	public int PackageFileId { get; }
	public string PackageName { get; }
	public int PackageId { get; }
	public int ProjectId { get; }
	public string Version { get; }
	public string Filename { get; }
	public GitLabSymbolPackage(int projectId, int packageId, int packageFileId, string packageName, string version, string filename) {
		PackageFileId = packageFileId;
		PackageId = packageId;
		PackageName = packageName;
		ProjectId = projectId;
		Version = version;
		Filename = filename;
	}
}