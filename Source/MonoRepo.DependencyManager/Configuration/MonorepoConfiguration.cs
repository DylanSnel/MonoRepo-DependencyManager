namespace MonoRepo.DependencyManager.Configuration;

public class MonorepoConfiguration
{

    public string BuildPipelinesFileExtension { get; set; } = "*.Build.yml";
    public string PolicyPipelinesFileExtension { get; set; } = "*.PullRequest.yml";
    public bool UseDifferentPolicyPipelines { get; set; }
    public BuildFileConfiguration BuildFiles { get; set; } = new();
    public DevopsConnection AzureDevops { get; set; } = new();
    public DockerConfiguration Docker { get; set; } = new();
}
