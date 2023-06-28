using Microsoft.Build.Construction;
using MonoRepo.DependencyManager.Helpers;

namespace MonoRepo.DependencyManager.Models;

public class ProjectFile
{
    public ProjectFile(string path)
    {
        FullPath = path;
        DockerFiles = FileHelper.GetFilesByType(FullDirectory, Global.Config.Docker.DockerFileExtension);
        ProjectReferences = getProjectReferences();
        BuildPipelines = FileHelper.GetFilesByType(FullDirectory, Global.Config.BuildPipelinesFileExtension).Select(bp => new Pipeline(bp, RelativeSolutionDirectory)).ToList();
        PolicyPipelines = FileHelper.GetFilesByType(FullDirectory, Global.Config.PolicyPipelinesFileExtension).Select(prp => new Pipeline(prp, RelativeSolutionDirectory)).ToList();
    }

    public string FullPath { get; set; }
    public List<string> DockerFiles { get; private set; }
    public List<Pipeline> BuildPipelines { get; private set; }
    public List<Pipeline> PolicyPipelines { get; }
    public List<ProjectFile> ProjectReferences { get; private set; }
    public List<string> BuildProjectReferences
    {
        get
        {
            var references = ProjectReferences.Select(pr => pr.RelativeDirectory.Replace("\\", "/").TrimEnd('/') + "/*").ToList();
            references.Add(RelativeDirectory.ForwardSlashes().TrimEnd('/') + "/*");
            foreach (var reference in ProjectReferences)
            {
                references.AddRange(reference.BuildProjectReferences);
            }
            references.AddRange(Global.Config.BuildFiles.AdditionalPipelinesTriggerPaths);
            return references.Distinct().Order().ToList();
        }
    }

    public string FullDirectory
    {
        get
        {
            return Path.GetDirectoryName(FullPath) + "\\";
        }
    }
    public string ProjectFileName
    {
        get
        {
            return Path.GetFileName(FullPath);
        }
    }

    public string RelativePath
    {
        get
        {
            return FullPath.Replace(Global.RootPath, "").TrimStart('\\');
        }
    }

    public string RelativeDirectory
    {
        get
        {
            return FullDirectory.Replace(Global.RootPath, "").TrimStart('\\');
        }
    }

    /// <summary>
    /// Path relative to the solution
    /// </summary>
    public string RelativeSolutionDirectory
    {
        get
        {
            var directory = new DirectoryInfo(FullDirectory);
            while (directory != null && !directory.GetFiles("*.sln").Any())
            {
                directory = directory.Parent;
            }
            return directory.FullName.Replace(Global.RootPath, "").TrimStart('\\');
        }
    }

    private List<ProjectFile> getProjectReferences()
    {
        var projectRootElement = ProjectRootElement.Open(FullPath);
        return projectRootElement.Items.Where(i => i.ItemType == "ProjectReference").Select(pr => new ProjectFile(Path.GetFullPath(Path.Combine(FullDirectory, pr.Include)))).ToList();
    }

    public void UpdateBuildPaths() => BuildPipelines.ForEach(bf => bf.SetDependencies(BuildProjectReferences));
}
