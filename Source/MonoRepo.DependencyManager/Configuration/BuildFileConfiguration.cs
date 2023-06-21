namespace MonoRepo.DependencyManager.Configuration;
public class BuildFileConfiguration
{
    public bool Enabled { get; set; } = true;
    public string BuildPipelinesFileExtension { get; set; } = "*.Build.yml";
    public bool UseSeparatePolicyPipelines { get; set; } = false;
    public string PolicyPipelinesFileExtension { get; set; } = "*.PullRequest.yml";
}
