using CommandLine;
using MonoRepo.DependencyManager.Commands.Interfaces;
using MonoRepo.DependencyManager.Configuration;
using MonoRepo.DependencyManager.Helpers;
using MonoRepo.DependencyManager.Integration;
using Newtonsoft.Json;
using rohankapoor.AutoPrompt;

namespace MonoRepo.DependencyManager.Commands;

[Verb("init", HelpText = "Initialize monorepo configuration")]
internal class InitCommand : ICommand
{
    [Option("overwrite", Required = false, HelpText = "Overwrite existing configuration", Default = false)]
    public bool OverWrite { get; set; }
    [Option("azure-devops", Required = false, HelpText = "Enable Azure Devops")]
    public bool? AzureDevops { get; set; } = null;
    [Option("docker", Required = false, HelpText = "Enable Docker Caching")]
    public bool? Docker { get; set; } = null;

    [Option("build-files", Required = false, HelpText = "Enable BuildFileUpdate")]
    public bool? BuildFiles { get; set; } = null;

    private readonly MonorepoConfiguration _config = Global.Config;
    public void Execute()
    {

        var filePath = Path.Combine(Global.RootPath, Global.ConfigFileName);
        if (File.Exists(filePath) && !OverWrite)
        {
            ColorConsole.WriteEmbeddedColorLine("Configuration file already exists. Use --overwrite to overwrite");
            return;
        }

        ConfigureBuildFiles();


        if (!Docker.HasValue)
        {
            Docker = Cli.Confirm("Do you want to use [DarkGreen]Docker Caching[/DarkGreen]?", _config.Docker.Enabled);
        }

        ConfigureDocker();

        if (!AzureDevops.HasValue)
        {
            AzureDevops = Cli.Confirm("Do you want to use [blue]Azure Devops[/blue]?", _config.AzureDevops.Enabled);
        }


        if (AzureDevops.Value)
        {
            _config.AzureDevops.Enabled = true;
            ConfigureAzureDevopsConnection();
            ConfigureAzureDevopsSettings();
        }

        File.WriteAllText(Path.Combine(Global.RootPath, Global.ConfigFileName), JsonConvert.SerializeObject(_config, Formatting.Indented));
        ColorConsole.WriteEmbeddedColorLine("Configuration file created");

    }

    private void ConfigureDocker()
    {
        if (Docker.Value)
        {
            _config.Docker.Enabled = true;
            _config.Docker.DockerFileExtension = Cli.AskFor<string>("What is the extention of your [DarkGreen]Docker Files[/DarkGreen]?", _config.Docker.DockerFileExtension);
        }
    }

    private void ConfigureBuildFiles()
    {
        _config.BuildFiles.BuildPipelinesFileExtension = Cli.AskFor<string>("What is the extension of your [DarkRed]Build Pipelines[/DarkRed]?", _config.BuildFiles.BuildPipelinesFileExtension);

        if (!BuildFiles.HasValue)
        {
            BuildFiles = Cli.Confirm("Do you want to update [DarkRed]Build Pipelines[/DarkRed] with the correct triggers?", _config.BuildFiles.Enabled);
        }
        _config.BuildFiles.Enabled = BuildFiles.Value;

        _config.BuildFiles.UseSeparatePolicyPipelines = Cli.Confirm("Do you want to use different [red]Policy Pipelines[/red]?", _config.BuildFiles.UseSeparatePolicyPipelines);
        if (_config.BuildFiles.UseSeparatePolicyPipelines)
        {
            _config.BuildFiles.PolicyPipelinesFileExtension = Cli.AskFor<string>("What is the extension of your [red]Policy Pipelines[/red]?", _config.BuildFiles.PolicyPipelinesFileExtension);
        }

        string additionalPipelines;
        while ((additionalPipelines = Cli.AskFor<string>("You you want to add an additional file extensions to include that do not fall under a project?  [Leave empty to continue]")) != string.Empty)
        {
            _config.BuildFiles.AdditionalPipelinesFileExtension.Add($"{(additionalPipelines.StartsWith("*.") ? "" : "*.")}{additionalPipelines}");
        }
        _config.BuildFiles.AdditionalPipelinesFileExtension = _config.BuildFiles.AdditionalPipelinesFileExtension.Distinct().ToList();


        string additionalTriggerPaths;
        while ((additionalTriggerPaths = Cli.AskFor<string>("You you want to add an additional file extensions to include that do not fall under a project? ( *.<MyExtension>.yaml ) [Leave empty to continue]")) != string.Empty)
        {
            additionalTriggerPaths = additionalTriggerPaths.Replace('\\', '/').TrimEnd('/');
            _config.BuildFiles.AdditionalPipelinesTriggerPaths.Add($"{(additionalTriggerPaths.StartsWith("/") ? "" : "/")}{additionalTriggerPaths}{(additionalTriggerPaths.EndsWith("*") ? "" : "/*")}");
        }
        _config.BuildFiles.AdditionalPipelinesTriggerPaths = _config.BuildFiles.AdditionalPipelinesFileExtension.Distinct().ToList();
    }

    private void ConfigureAzureDevopsConnection()
    {
        _config.AzureDevops.AzureDevopsUrl = Cli.AskFor<string>("What is the url of your [blue]Azure Devops[/blue] organization?", _config.AzureDevops.AzureDevopsUrl);

        ColorConsole.WriteEmbeddedColorLine("");
        var checkPermissions = new CheckDevopsPermissionsCommand();
        checkPermissions.Execute();

        var azureClient = new AzureDevopsClient();


        ColorConsole.WriteEmbeddedColorLine("");
        ColorConsole.WriteEmbeddedColor("Connecting to [blue]{_config.AzureDevops.AzureDevopsUrl}[/blue]: ");
        var projects = azureClient.GetProjects();
        if (projects.Count == 0)
        {
            ColorConsole.WriteEmbeddedColorLine("[red]No projects found[/red]");
            return;
        }
        else if (projects.Count == 1)
        {
            ColorConsole.WriteEmbeddedColorLine($"Found [green]{projects.Count}[/green] project");
            _config.AzureDevops.ProjectName = projects.First().Name;
            ColorConsole.WriteEmbeddedColorLine($"[blue]Automatically assigned {_config.AzureDevops.ProjectName}[/blue]");
        }
        else
        {
            ColorConsole.WriteEmbeddedColorLine($"Found [green]{projects.Count}[/green] projects");
            ColorConsole.WriteEmbeddedColorLine("");
            _config.AzureDevops.ProjectName = AutoPrompt.PromptForInput_Searchable("What is the name of your project? (Up/down/type)  ", projects.Select(p => p.Name).ToArray());
        }

        var repositories = azureClient.GetRepositories();
        if (repositories.Count == 0)
        {
            ColorConsole.WriteEmbeddedColorLine("[red]No repositories found[/red]");
            return;
        }
        else if (repositories.Count == 1)
        {
            ColorConsole.WriteEmbeddedColorLine($"Found [green]{repositories.Count}[/green] repository");

            _config.AzureDevops.RepositoryName = repositories.First().Name;
            ColorConsole.WriteEmbeddedColorLine($"[blue]Automatically assigned {_config.AzureDevops.RepositoryName}[/blue]");
        }
        else
        {
            ColorConsole.WriteEmbeddedColorLine($"Found [green]{repositories.Count}[/green] repositories");
            ColorConsole.WriteEmbeddedColorLine("");
            _config.AzureDevops.RepositoryName = AutoPrompt.PromptForInput_Searchable("What is the name of your repository? (Up/down/type)  ", repositories.Select(p => p.Name).ToArray());
        }
    }

    private void ConfigureAzureDevopsSettings()
    {
        var azureClient = new AzureDevopsClient();
        var repository = azureClient.GetRepository();
        _config.AzureDevops.Settings.MainBranch = Cli.AskFor<string>("What is the name of your main branch?", _config.AzureDevops.Settings.MainBranch ?? repository.DefaultBranch);


        string additionalBranch;
        while ((additionalBranch = Cli.AskFor<string>("You you want to add an additional policy branch? [Leave empty to continue]")) != string.Empty)
        {
            _config.AzureDevops.Settings.PolicyBranches.Add($"{(additionalBranch.StartsWith("refs/heads/") ? "" : "refs/heads/")}{additionalBranch}");
        }
        _config.AzureDevops.Settings.PolicyBranches = _config.AzureDevops.Settings.PolicyBranches.Distinct().ToList();

        _config.AzureDevops.Settings.AutoImportBuildPipelines = Cli.Confirm("Do you want to automatically import [DarkRed]Build Pipelines[/DarkRed]?", _config.AzureDevops.Settings.AutoImportBuildPipelines);
        if (_config.AzureDevops.Settings.AutoImportBuildPipelines)
        {
            ColorConsole.WriteEmbeddedColorLine($"The tool will structure your pipelines in the same folder structure in devops as your folder structure.");
            ColorConsole.WriteEmbeddedColorLine($"If you want, you can to omit a prefix from this folder structure (for instance 'Source'or src) so that your root folder isnt useless.");
            _config.AzureDevops.Settings.OmitFolderFromPipelineDirectory = Cli.AskFor<string>("What is the name of the folder you want to omit from the pipeline directory? (Empty to skip)", _config.AzureDevops.Settings.OmitFolderFromPipelineDirectory);
        }
        if (_config.UseDifferentPolicyPipelines)
        {
            _config.AzureDevops.Settings.AutoImportPolicyPipelines = Cli.Confirm("Do you want to automatically import [red]Policy Pipelines[/red]?", _config.AzureDevops.Settings.AutoImportPolicyPipelines);
        }

        _config.AzureDevops.Settings.AgentPoolName = Cli.AskFor<string>("What is the name of your [blue]agent pool[/blue]?", _config.AzureDevops.Settings.AgentPoolName);

        // _config.AzureDevops.Settings.CreateDependencyManagerPipeline = Cli.Confirm("Do you want to create and auto import a dependency manager pipeline?", _config.AzureDevops.Settings.CreateDependencyManagerPipeline);
    }
}
