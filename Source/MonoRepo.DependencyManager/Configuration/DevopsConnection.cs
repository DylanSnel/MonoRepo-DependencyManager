using System.Collections.Generic;

namespace MonoRepo.DependencyManager.Configuration;

public class DevopsConnection
{
    public bool Enabled { get; set; }

    public string AzureDevopsUrl { get; set; } = "https://dev.azure.com/<your-organisation>/";
    public string ProjectName { get; set; }
    public string RepositoryName { get; set; }

    public DevopsSettings Settings { get; set; } = new();
}

public class DevopsSettings
{
    public string MainBranch { get; set; } = null;
    public List<string> PolicyBranches { get; set; } = new();
    public bool AutoImportBuildPipelines { get; set; } = true;
    public bool AutoImportPolicyPipelines { get; set; }
    public string AgentPoolName { get; set; } = "Azure Pipelines";
    public string OmitFolderFromPipelineDirectory { get; set; } = "";

    public bool CreateDependencyManagerPipeline { get; set; }
}
