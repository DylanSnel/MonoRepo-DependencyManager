namespace MonoRepo.DependencyManager.Configuration;

public class DockerConfiguration
{
    public string DockerFileExtension { get; set; } = "DockerFile";
    public bool Enabled { get; set; }
}
