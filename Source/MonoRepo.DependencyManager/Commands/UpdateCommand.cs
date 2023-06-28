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

        if (!Program.CheckForConfiguration())
        {
            return;
        }

        if (Global.Config.AzureDevops.Enabled)
        {
            _azureClient = new AzureDevopsClient();
        }


        foreach (var solution in Global.Solutions)
        {
            ColorConsole.WriteEmbeddedColorLine($"Solution: [magenta]{solution.SolutionName}[/magenta]");
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

            if (project.DockerFiles.Any())
            {
                ColorConsole.WriteEmbeddedColorLine($" {"".PadRight(level * 2)}   - Docker Files: ");
                foreach (var dockerFile in project.DockerFiles)
                {
                    if (DockerLogic.ReplaceDockerReferences(project, dockerFile))
                    {
                        ColorConsole.WriteEmbeddedColorLine($" {"".PadRight(level * 2)}     [DarkGreen]{Path.GetFileName(dockerFile)}[/DarkGreen]: [green]Updated[/green]");
                    }
                    else
                    {
                        ColorConsole.WriteEmbeddedColorLine($" {"".PadRight(level * 2)}     [DarkGreen]{Path.GetFileName(dockerFile)}[/DarkGreen]: [red]Failed[/red]");
                    }
                }
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
                    var result = _azureClient.CreateOrUpdateBuildDefinition(solution, pipeline, $"{pipeline.BuildName} - {pipeline.ExtensionName}", project.BuildProjectReferences);
                    ColorConsole.WriteEmbeddedColorLine($" {"".PadRight(level * 2)}     - {(result ? "Pipeline imported" : "Already imported")}");
                }

                if (Global.Config.AzureDevops.Enabled && !Global.Config.BuildFiles.UseSeparatePolicyPipelines)
                {
                    var branch = $"refs/heads/{Global.CurrentBranch}";
                    var required = Global.Config.AzureDevops.Settings.MainBranch == branch;
                    CreateOrUpdatePolicy(level, project, pipeline, required, Global.Config.AzureDevops.Settings.MainBranch);

                    foreach (var policyBranch in Global.Config.AzureDevops.Settings.PolicyBranches)
                    {
                        CreateOrUpdatePolicy(level, project, pipeline, required, policyBranch);
                    }
                }
            }

            if (Global.Config.BuildFiles.UseSeparatePolicyPipelines)
            {
                foreach (var pipeline in project.PolicyPipelines)
                {
                    ColorConsole.WriteEmbeddedColorLine($" {"".PadRight(level * 2)}   - Policy Pipeline: [DarkBlue]{pipeline.FileName}[/DarkBlue]");

                    if (Global.Config.AzureDevops.Enabled && Global.Config.AzureDevops.Settings.AutoImportPolicyPipelines)
                    {
                        var result = _azureClient.CreateOrUpdateBuildDefinition(solution, pipeline, $"{pipeline.BuildName} - {pipeline.ExtensionName}", project.BuildProjectReferences);
                        ColorConsole.WriteEmbeddedColorLine($" {"".PadRight(level * 2)}     - {(result ? "Pipeline imported" : "Already imported")}");
                    }

                    if (Global.Config.AzureDevops.Enabled && Global.Config.BuildFiles.UseSeparatePolicyPipelines)
                    {
                        var branch = $"refs/heads/{Global.CurrentBranch}";
                        var required = Global.Config.AzureDevops.Settings.MainBranch == branch;
                        CreateOrUpdatePolicy(level, project, pipeline, required, Global.Config.AzureDevops.Settings.MainBranch);

                        foreach (var policyBranch in Global.Config.AzureDevops.Settings.PolicyBranches)
                        {
                            CreateOrUpdatePolicy(level, project, pipeline, required, policyBranch);
                        }
                    }
                }
            }
            UpdateProjects(solution, project.ProjectReferences, level + 1);
        }
    }

    private bool CreateOrUpdatePolicy(int level, ProjectFile project, Pipeline pipeline, bool required, string policyBranch)
    {
        var result = _azureClient.CreateOrUpdatePolicy(project, pipeline, policyBranch, required);
        ColorConsole.WriteEmbeddedColorLine($" {"".PadRight(level * 2)}     - {policyBranch.Replace("refs/heads/", "")} {(result.Item1 ? "Policy Applied" : "Policy Updated")} {(result.Item2 ? "Required" : "Optional")}");
        return result.Item1;
    }
}
