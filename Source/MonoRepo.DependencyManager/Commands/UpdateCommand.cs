using CommandLine;
using MonoRepo.DependencyManager.Commands.Interfaces;
using MonoRepo.DependencyManager.Helpers;
using MonoRepo.DependencyManager.Integration;
using MonoRepo.DependencyManager.Models;

namespace MonoRepo.DependencyManager.Commands;

[Verb("update", HelpText = "Add configuration file to the root repo")]
public class UpdateCommand : ICommand
{
    private AzureDevopsClient _azureClient;

    [Option('v', "verbose", Required = false, HelpText = "Verbose", Default = false)]
    public bool Verbose { get; set; }

    public void Execute()
    {

        _azureClient = new AzureDevopsClient();
        foreach (var solution in Global.Solutions)
        {
            ColorConsole.WriteEmbeddedColorLine($"Solution: [blue]{solution.SolutionName}[/blue]");
            UpdateProjects(solution, solution.Projects, 1);
        }

    }

    private void UpdateProjects(SolutionFile solution, List<ProjectFile> projects, int level)
    {
        foreach (var project in projects)
        {
            if (Verbose
                || project.DockerFiles.Any()
                || project.BuildPipelines.Any()
                || project.PolicyPipelines.Any()
                )
            {
                ColorConsole.WriteEmbeddedColorLine($" {"".PadRight(level * 2)} - Project: [yellow]{project.ProjectFileName}[/yellow]");
            }

            foreach (var pipeline in project.BuildPipelines)
            {
                ColorConsole.WriteEmbeddedColorLine($" {"".PadRight(level * 2)}   - Build Pipeline: [DarkRed]{pipeline.FileName}[/DarkRed]");
                if (Global.Config.BuildFiles.Enabled)
                {
                    pipeline.SetDependencies(project.BuildProjectReferences);
                    ColorConsole.WriteEmbeddedColorLine($" {"".PadRight(level * 2)}     - References Updated");
                }

                if (Global.Config.AzureDevops.Enabled && Global.Config.AzureDevops.Settings.AutoImportBuildPipelines)
                {
                    var result = _azureClient.CreatBuildDefinition(solution, pipeline, $"{pipeline.BuildName} - Build", project.BuildProjectReferences);
                    ColorConsole.WriteEmbeddedColorLine($" {"".PadRight(level * 2)}     - {(result ? "Pipeline imported" : "Already imported")}");
                }

                if (Global.Config.AzureDevops.Enabled && !Global.Config.BuildFiles.UseSeparatePolicyPipelines)
                {


                    //var result = _azureClient.CreateNewPolicy(solution, pipeline, $"{pipeline.BuildName} - Build", project.BuildProjectReferences);
                    //ColorConsole.WriteEmbeddedColorLine($" {"".PadRight(level * 2)}     - {(result ? "Pipeline imported" : "Already imported")}");
                }
            }

            //foreach (var pipeline in project.BuildPipelines)
            //{
            //    ColorConsole.WriteEmbeddedColorLine($" {"".PadRight(level * 2)}   - Build Pipeline: [DarkRed]{pipeline.FileName}[/DarkRed]");
            //    if (Global.Config.BuildFiles.Enabled)
            //    {
            //        pipeline.SetDependencies(project.BuildProjectReferences);
            //        ColorConsole.WriteEmbeddedColorLine($" {"".PadRight(level * 2)}     - References Updated");
            //    }

            //    if (Global.Config.AzureDevops.Enabled && Global.Config.AzureDevops.Settings.AutoImportBuildPipelines)
            //    {
            //        var result = _azureClient.CreatBuildDefinition(solution, pipeline, $"{pipeline.BuildName} - Build", project.BuildProjectReferences);
            //        ColorConsole.WriteEmbeddedColorLine($" {"".PadRight(level * 2)}     - {(result ? "Pipeline imported" : "Already imported")}");
            //    }
            //}

            //if (Global.Config.AzureDevops.Enabled && Global.Config.AzureDevops.Settings.AutoImportBuildPipelines)
            //{
            //    var result = _azureClient.CreatBuildDefinition(solution, pipeline, $"{pipeline.BuildName} - Policy", project.BuildProjectReferences);
            //    ColorConsole.WriteEmbeddedColorLine($" {"".PadRight(level * 2)}     - {(result ? "Policy Pipeline imported" : "Already imported")}");
            //}

            project.DockerFiles.ForEach(dockerfile => ColorConsole.WriteEmbeddedColorLine($" {"".PadRight(level * 2)}   - Dockerfile: [DarkGreen]{Path.GetFileName(dockerfile)}[/DarkGreen]"));
            project.BuildPipelines.ForEach(bp => ColorConsole.WriteEmbeddedColorLine($" {"".PadRight(level * 2)}   - Build Pipeline: [DarkRed]{bp.FileName}[/DarkRed]"));
            project.PolicyPipelines.ForEach(prp => ColorConsole.WriteEmbeddedColorLine($" {"".PadRight(level * 2)}   - Policy Pipeline: [DarkBlue]{prp.FileName}[/DarkBlue]"));
            UpdateProjects(solution, project.ProjectReferences, level + 1);

        }
    }
}
