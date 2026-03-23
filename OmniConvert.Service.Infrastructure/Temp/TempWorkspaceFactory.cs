namespace OmniConvert.Service.Infrastructure.Temp;

using OmniConvert.Service.Core.Interfaces;

public class TempWorkspaceFactory : ITempWorkspaceFactory
{
    private readonly string _baseDir =
        Path.Combine(Path.GetTempPath(), "OmniConvert", "workspaces");

    public string CreateWorkspace(Guid jobId)
    {
        var path = Path.Combine(_baseDir, jobId.ToString());
        Directory.CreateDirectory(path);
        return path;
    }

    public void CleanupWorkspace(Guid jobId)
    {
        var path = Path.Combine(_baseDir, jobId.ToString());
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}