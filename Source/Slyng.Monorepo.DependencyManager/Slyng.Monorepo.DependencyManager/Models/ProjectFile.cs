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
            Console.WriteLine($"Project: {ProjectFileName}");
            DockerFiles = FileHelper.GetFilesByType(FullDirectory, "DockerFile");
            BuildFiles = FileHelper.GetFilesByType(FullDirectory, "*.build.yml").Select(bf => new BuildFile(bf, RelativeSolutionDirectory)).ToList();
            ProjectReferences = getProjectReferences();
        }


        public string FullPath { get; set; }
        public List<string> DockerFiles { get; private set; }
        public List<BuildFile> BuildFiles { get; private set; }

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

        public void HandleReplaceMents()
        {
            Console.WriteLine($"Replacing: {ProjectFileName}");
            replaceDockerReferences();
            replaceBuildPaths();
        }

        private List<string> getProjectReferences()
        {
            var projectCollection = new ProjectCollection();

            var project = projectCollection.LoadProject(FullPath);
            var projectReferences = project.GetItems("ProjectReference");
            return projectReferences.Select(pr => new ProjectFile(Path.GetFullPath(Path.Combine(FullDirectory, pr.EvaluatedInclude))).RelativeDirectory.Replace("\\", "/").TrimEnd('/') + "/*").Distinct().ToList();
            

        }

        private void replaceBuildPaths()
        {
            // ProjectReferences = getProjectReferences();
            BuildFiles.ForEach(bf => bf.SetDependencies(ProjectReferences));
        }

        private void replaceDockerReferences()
        {
            var replacement = new StringBuilder();
            replacement.AppendLine($"WORKDIR /src");
            foreach (var solution in Global.Solutions)
            {
                //replacement.AppendLine($"COPY [\"{solution.RelativePath.ForwardSlashes()}\", \"{solution.RelativeDirectory.ForwardSlashes()}\"]");
                foreach (var project in solution.Projects)
                {
                    replacement.AppendLine($"COPY [\"{project.RelativePath.ForwardSlashes()}\", \"{project.RelativeDirectory.ForwardSlashes()}\"]");
                }
                // replacement.AppendLine($"");
            }
            replacement.AppendLine($"RUN dotnet restore \"{RelativePath.ForwardSlashes()}\"");
            replacement.AppendLine($"COPY . .");
            replacement.AppendLine($"WORKDIR \"/src/{RelativeDirectory.ForwardSlashes()}\"");

            foreach (var dockerFile in DockerFiles)
            {
                var text = File.ReadAllText(dockerFile);
                var explodedText = text.Split(new[] { "#DependencyToolGeneratedCode", "#End" }, StringSplitOptions.None);
                if (explodedText.Length != 3)
                {
                    Console.WriteLine($"Error replacing dependencies in DockerFile ");
                    continue;
                }
                explodedText[1] = replacement.ToString();

                var newText = string.Join("", new string[] { explodedText[0], "#DependencyToolGeneratedCode", "\r\n", explodedText[1], "\r\n", "#End", explodedText[2] });
                File.WriteAllText(dockerFile, newText);
            }



        }


    }
}
