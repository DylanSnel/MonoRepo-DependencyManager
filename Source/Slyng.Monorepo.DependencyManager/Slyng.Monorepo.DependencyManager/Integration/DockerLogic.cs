using Slyng.Monorepo.DependencyManager.Helpers;
using Slyng.Monorepo.DependencyManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slyng.Monorepo.DependencyManager.Integration
{
    public class DockerLogic
    {

        public static void UpdateDocker()
        {
            Console.WriteLine();
            ColorConsole.WriteEmbeddedColorLine($"[green]Updating Docker Files[/green]");
            foreach (var solution in Global.Solutions)
            {
                ColorConsole.WriteEmbeddedColorLine($"Solution: [blue]{solution.SolutionFileName}[/blue]");
                foreach (var project in solution.Projects)
                {
                    try
                    {
                        if (project.DockerFiles.Any())
                        {
                            replaceDockerReferences(project);
                            ColorConsole.WriteEmbeddedColorLine($"- [yellow]{project.ProjectFileName}[/yellow] - [green]Complete[/green]");
                        }
                        else
                        {
                            ColorConsole.WriteEmbeddedColorLine($"- [yellow]{project.ProjectFileName}[/yellow] - No dockerfile");
                        }
                    }
                    catch (Exception ex)
                    {
                        ColorConsole.WriteLine($"Error replacing dependencies in DockerFile for project {project.ProjectFileName}", ConsoleColor.Red);
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        private static void replaceDockerReferences(ProjectFile projectFile)
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
            replacement.AppendLine($"RUN dotnet restore \"{projectFile.RelativePath.ForwardSlashes()}\"");
            replacement.AppendLine($"COPY . .");
            replacement.AppendLine($"WORKDIR \"/src/{projectFile.RelativeDirectory.ForwardSlashes()}\"");

            foreach (var dockerFile in projectFile.DockerFiles)
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
