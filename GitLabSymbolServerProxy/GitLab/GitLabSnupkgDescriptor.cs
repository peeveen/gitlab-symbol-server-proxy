namespace GitLabSymbolServerProxy;

public class GitLabSnupkgDescriptor : ISnupkgDescriptor {
	public GitLabSnupkgDescriptor(string packageName, string filename, string version, int projectId, int packageId, int packageFileId) {
		PackageName = packageName;
		Filename = filename;
		Version = version;

		ProjectId = projectId;
		PackageId = packageId;
		PackageFileId = packageFileId;
	}

	public string PackageName { get; }
	public string Filename { get; }
	public string Version { get; }

	public int ProjectId { get; }
	public int PackageId { get; }
	public int PackageFileId { get; }
}