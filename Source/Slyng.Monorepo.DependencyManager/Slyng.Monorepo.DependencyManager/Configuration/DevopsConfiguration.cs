namespace Slyng.Monorepo.DependencyManager.Configuration
{
    public class DevopsConfiguration
    {
        public bool Enabled { get; set; }

        public bool AutoImportBuildPipelines { get; set; }
        public bool UpdateBuildPipelinesReferences { get; set; }
        public bool AutoImportPrPipelines { get; set; }
        public bool UsePrPipelinesAsBranchPolicies { get; set; }

        public string AgentPoolName { get; set; } = "Azure Pipelines";

        public string BuildPipelinesFileExtension { get; set; } = "*.Build.yml";
        public string PrPipelinesFileExtension { get; set; } = "*.PullRequest.yml";

        public string OmitFolderFromPipelineDirectory { get; set; } = "";
    }
}
