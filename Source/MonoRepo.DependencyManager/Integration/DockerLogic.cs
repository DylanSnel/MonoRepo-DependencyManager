using MonoRepo.DependencyManager.Helpers;
using MonoRepo.DependencyManager.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace MonoRepo.DependencyManager.Integration;

public class DockerLogic
{

    public static void UpdateDocker()
    {
        ColorConsole.WriteEmbeddedColorLine("");
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
                        ReplaceDockerReferences(project);
                        ColorConsole.WriteEmbeddedColorLine($"- [yellow]{project.ProjectFileName}[/yellow] - [green]Complete[/green]");
                    }
                    else
                    {
                        ColorConsole.WriteEmbeddedColorLine($"- [yellow]{project.ProjectFileName}[/yellow] - No dockerfile");
                    }
                }
                catch (Exception ex)
                {
                    ColorConsole.WriteEmbeddedColorLine($"Error replacing dependencies in DockerFile for project {project.ProjectFileName}", ConsoleColor.Red);
                    ColorConsole.WriteEmbeddedColorLine(ex.Message);
                }
            }
        }
    }

    public static void ReplaceDockerReferences(ProjectFile projectFile)
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
                ColorConsole.WriteEmbeddedColorLine($"Error replacing dependencies in DockerFile ");
                continue;
            }
            explodedText[1] = replacement.ToString();

            var newText = string.Join("", new string[] { explodedText[0], "#DependencyToolGeneratedCode", "\r\n", explodedText[1], "\r\n", "#End", explodedText[2] });
            File.WriteAllText(dockerFile, newText);
        }
    }
}
