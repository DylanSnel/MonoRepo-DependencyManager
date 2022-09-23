using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Utilities;
using Slyng.Monorepo.DependencyManager.Helpers;
using System.Xml;
using System.Xml.Linq;

namespace Slyng.Monorepo.DependencyManager.Models
{
    public class ProjectFile
    {
        public ProjectFile(string path)
        {
            FullPath = path;
            ColorConsole.WriteEmbeddedColorLine($" - Project: [yellow]{ProjectFileName}[/yellow]");
            DockerFiles = FileHelper.GetFilesByType(FullDirectory, Global.Config.DockerFileExtention);
            BuildPipelines = FileHelper.GetFilesByType(FullDirectory, Global.Config.AzureDevops.BuildPipelinesFileExtension).Select(bp => new Pipeline(bp, RelativeSolutionDirectory)).ToList();
            PrPipelines = FileHelper.GetFilesByType(FullDirectory, Global.Config.AzureDevops.PrPipelinesFileExtension).Select(prp => new Pipeline(prp, RelativeSolutionDirectory)).ToList();
            ProjectReferences = getProjectReferences();
        }


        public string FullPath { get; set; }
        public List<string> DockerFiles { get; private set; }
        public List<Pipeline> BuildPipelines { get; private set; }
        public List<Pipeline> PrPipelines { get; }
        public List<string> ProjectReferences { get; private set; }

        public string FullDirectory
        {
            get
            {
                return Path.GetDirectoryName(FullPath) + "\\"; ;
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

        private List<string> getProjectReferences()
        {
            var projectCollection = new ProjectCollection();

            var project = projectCollection.LoadProject(FullPath);
            var projectReferences = project.GetItems("ProjectReference");
            var references = projectReferences.Select(pr => new ProjectReference(Path.GetFullPath(Path.Combine(FullDirectory, pr.EvaluatedInclude))).RelativeDirectory.Replace("\\", "/").TrimEnd('/') + "/*").ToList();

            references.Add(RelativeDirectory.Replace("\\", "/").TrimEnd('/') + "/*");

            return references.Distinct().ToList();

        }

        public void UpdateBuildPaths()
        {
            // ProjectReferences = getProjectReferences();
            BuildPipelines.ForEach(bf => bf.SetDependencies(ProjectReferences));
        }
    }
}
