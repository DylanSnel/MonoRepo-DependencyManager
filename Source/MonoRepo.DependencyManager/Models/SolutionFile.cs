using MonoRepo.DependencyManager.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MonoRepo.DependencyManager.Models;

public class SolutionFile
{
    public SolutionFile(string path)
    {
        FullPath = path;
        Projects = FileHelper.GetFilesByType(FullDirectory, "*.csproj").Where(csproj=> !csproj.EndsWith("Tests.csproj")).Select(csproj => new ProjectFile(csproj)).ToList();
    }

    public string FullPath { get; set; }
    public List<ProjectFile> Projects { get; private set; }

    public string FullDirectory
    {
        get
        {
            return Path.GetDirectoryName(FullPath) + "\\";
        }
    }

    public string SolutionName
    {
        get
        {
            return SolutionFileName.Replace(".sln", "");
        }
    }

    public string SolutionFileName
    {
        get
        {
            return Path.GetFileName(FullPath);
        }
    }
    /// <summary>
    /// Relative path from the root of the repository to the solution file.
    /// </summary>
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

    public string PipelineDirectory
    {
        get
        {
            if (string.IsNullOrEmpty(Global.Config.AzureDevops.Settings.OmitFolderFromPipelineDirectory))
            {
                return RelativeDirectory.TrimStart('\\');
            }
            return RelativeDirectory.Replace(Global.Config.AzureDevops.Settings.OmitFolderFromPipelineDirectory, "").TrimStart('\\');
        }
    }




}
