using CommandLine;
using MonoRepo.DependencyManager.Commands.Interfaces;
using MonoRepo.DependencyManager.Helpers;
using MonoRepo.DependencyManager.Models;

namespace MonoRepo.DependencyManager.Commands;

[Verb("show", HelpText = "Show project tree")]
public class ProjectTreeCommand : ICommand
{
    public void Execute()
    {
        foreach (var solution in Global.Solutions)
        {
            ColorConsole.WriteEmbeddedColorLine($"Solution: [magenta]{solution.SolutionName}[/magenta]");
            PrintProjects(solution.Projects, 1);
        }

    }

    private void PrintProjects(List<ProjectFile> projects, int level)
    {
        foreach (var project in projects)
        {
            ColorConsole.WriteEmbeddedColorLine($" {"".PadRight(level * 2)} - Project: [yellow]{project.ProjectFileName}[/yellow]");
            project.DockerFiles.ForEach(dockerfile => ColorConsole.WriteEmbeddedColorLine($" {"".PadRight(level * 2)}   - Dockerfile: [DarkGreen]{Path.GetFileName(dockerfile)}[/DarkGreen]"));
            project.BuildPipelines.ForEach(bp => ColorConsole.WriteEmbeddedColorLine($" {"".PadRight(level * 2)}   - Build Pipeline: [DarkRed]{bp.FileName}[/DarkRed]"));
            project.PolicyPipelines.ForEach(prp => ColorConsole.WriteEmbeddedColorLine($" {"".PadRight(level * 2)}   - Policy Pipeline: [DarkBlue]{prp.FileName}[/DarkBlue]"));
            PrintProjects(project.ProjectReferences, level + 1);
        }
    }
}
