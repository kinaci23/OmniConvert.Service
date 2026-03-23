namespace OmniConvert.Service.Core.Interfaces;

public interface ITempWorkspaceFactory
{
    string CreateWorkspace(Guid jobId);
    void CleanupWorkspace(Guid jobId);
}