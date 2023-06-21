﻿using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.TeamFoundation.Policy.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;
using MonoRepo.DependencyManager.Helpers;
using MonoRepo.DependencyManager.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MonoRepo.DependencyManager.Integration;

internal class AzureDevopsClient
{
    private VssConnection Connection
    {
        get
        {
            if (_connection == null)
            {
                var creds = new VssOAuthAccessTokenCredential(Global.DevopsAccessToken);
                _connection = new VssConnection(new Uri(Global.Config.AzureDevops.AzureDevopsUrl), creds);
            }
            return _connection;
        }
    }

    public void ResetConnection()
    {
        if (!string.IsNullOrEmpty(Global.DevopsAccessToken) && !string.IsNullOrEmpty(Global.Config.AzureDevops.AzureDevopsUrl))
        {
            var creds = new VssOAuthAccessTokenCredential(Global.DevopsAccessToken);
            _connection = new VssConnection(new Uri(Global.Config.AzureDevops.AzureDevopsUrl), creds);
        }
    }


    private VssConnection _connection;

    private readonly GitRepository _repository;
    private readonly List<BuildDefinition> _builds;
    private readonly List<PolicyConfiguration> _policies;
    private readonly List<TaskAgentQueue> _queues;
    private readonly Guid _buildPolicyType = new("0609b952-1397-4640-95ec-e00a01b2c241");

    public AzureDevopsClient()
    {
        _repository = GetRepository();
        _builds = GetAllBuilds();
        _policies = GetAllPolicies();
        _queues = GetAgentPool();
    }
    public T GetClient<T>() where T : VssHttpClientBase
        => Connection.GetClient<T>();
    public List<TeamProjectReference> GetProjects()
    {
        using var gitClient = GetClient<ProjectHttpClient>();
        return gitClient.GetProjects().Result.ToList();
    }

    public List<GitRepository> GetRepositories(string projectName = null)
    {
        projectName ??= Global.Config.AzureDevops.ProjectName;
        using var gitClient = GetClient<GitHttpClient>();
        return gitClient.GetRepositoriesAsync(projectName).Result;
    }

    public GitRepository GetRepository(string projectName = null, string repositoryName = null)
    {
        projectName ??= Global.Config.AzureDevops.ProjectName;
        repositoryName ??= Global.Config.AzureDevops.RepositoryName;
        using var gitClient = GetClient<GitHttpClient>();
        return gitClient.GetRepositoryAsync(projectName, repositoryName).Result;
    }

    public List<BuildDefinition> GetAllBuilds()
    {
        using var buildClient = GetClient<BuildHttpClient>();

        return buildClient.GetFullDefinitionsAsync(project: Global.Config.AzureDevops.ProjectName, repositoryId: _repository.Id.ToString(), repositoryType: "TfsGit").Result;
    }

    public List<TaskAgentQueue> GetAgentPool()
    {
        using var buildClient = GetClient<TaskAgentHttpClient>();
        return buildClient.GetAgentQueuesByNamesAsync(project: Global.Config.AzureDevops.ProjectName, new List<string> { Global.Config.AzureDevops.Settings.AgentPoolName }).Result;

    }

    public List<PolicyConfiguration> GetAllPolicies()
    {
        using var policyClient = GetClient<PolicyHttpClient>();
        return policyClient.GetPolicyConfigurationsAsync(project: Global.Config.AzureDevops.ProjectName, null, _buildPolicyType).Result;
    }

    //public void Update()
    //{

    //    ColorConsole.WriteEmbeddedColorLine("");
    //    ColorConsole.WriteEmbeddedColorLine($"[green]Updating Azure Devops[/green]");
    //    foreach (var solution in Global.Solutions)
    //    {
    //        ColorConsole.WriteEmbeddedColorLine($"Solution: [blue]{solution.SolutionFileName}[/blue]");
    //        foreach (var project in solution.Projects)
    //        {
    //            ColorConsole.WriteEmbeddedColorLine($" - Project: [yellow]{project.ProjectFileName}[/yellow]");
    //            try
    //            {
    //                if (_configuration.UpdateBuildPipelinesReferences)
    //                {
    //                    if (project.BuildPipelines.Any())
    //                    {
    //                        project.UpdateBuildPaths();
    //                        ColorConsole.WriteEmbeddedColorLine($"   - [darkyellow]Updating BuildPaths[/darkyellow] - [green]Complete[/green]");
    //                    }
    //                }

    //                if (_configuration.AutoImportBuildPipelines)
    //                {
    //                    foreach (var buildFile in project.BuildPipelines)
    //                    {
    //                        CreatBuildDefinition(solution, buildFile, $"{buildFile.BuildName} - Build", project.ProjectReferences);
    //                    }
    //                }

    //                if (_configuration.AutoImportPrPipelines)
    //                {
    //                    foreach (var buildFile in project.PrPipelines)
    //                    {
    //                        CreatBuildDefinition(solution, buildFile, $"{buildFile.BuildName} - PullRequest", project.ProjectReferences);
    //                    }
    //                }

    //                _builds = GetAllBuilds();

    //                if (_configuration.Devops.UsePrPipelinesAsBranchPolicies)
    //                {
    //                    foreach (var buildFile in project.PrPipelines)
    //                    {
    //                        CreateNewPolicy(project, buildFile);
    //                    }
    //                }

    //            }
    //            catch (Exception ex)
    //            {
    //                ColorConsole.WriteEmbeddedColorLine($"Error: [red]{ex.Message}[/red]");
    //            }
    //        }
    //    }
    //}

    public void CreateNewPolicy(ProjectFile project, Pipeline buildFile)
    {
        var build = _builds.FirstOrDefault(b => ((YamlProcess)b.Process).YamlFilename == buildFile.RelativePath.ForwardSlashes());
        if (build != null)
        {
            try
            {
                using var policyClient = GetClient<PolicyHttpClient>();
                var policy = _policies.FirstOrDefault(p => p.Settings.Value<int>("buildDefinitionId") == build.Id);
                if (policy == null)
                {
                    var scope = new Dictionary<string, object>()
                                                {
                                                    {"refName", _repository.DefaultBranch},
                                                    {"matchKind", "Exact"},
                                                    {"repositoryId", _repository.Id},
                                                };

                    var settings = JObject.FromObject(new Dictionary<string, object>()
                                                {
                                                    { "buildDefinitionId", build.Id },
                                                    { "queueOnSourceUpdateOnly", true },
                                                    { "manualQueueOnly", false },
                                                    { "displayName", $"{buildFile.BuildName} - PR"},
                                                    { "validDuration", 720.0d },
                                                    { "filenamePatterns", project.ProjectReferences.Select(x=> $"/{x}") },
                                                    { "scope", new [] {scope } }
                                                });

                    policy = new PolicyConfiguration()
                    {
                        IsEnabled = true,
                        Type = new PolicyTypeRef()
                        {
                            Id = _buildPolicyType
                        },
                        IsBlocking = true,
                        Settings = settings,

                    };
                    policyClient.CreatePolicyConfigurationAsync(policy, Global.Config.AzureDevops.ProjectName).Wait();
                    ColorConsole.WriteEmbeddedColorLine($"   - Add branch Policy: [DarkGreen]{buildFile.BuildName}[/DarkGreen] - [green]Complete[/green]");
                }
                else
                {
                    var scope = new Dictionary<string, object>()
                                                {
                                                    {"refName", _repository.DefaultBranch},
                                                    {"matchKind", "Exact"},
                                                    {"repositoryId", _repository.Id},
                                                };
                    var settings = JObject.FromObject(new Dictionary<string, object>()
                                                {
                                                    { "buildDefinitionId", build.Id },
                                                    { "queueOnSourceUpdateOnly", true },
                                                    { "manualQueueOnly", false },
                                                    { "displayName", $"{buildFile.BuildName} - PR"},
                                                    { "validDuration", 720.0d },
                                                    { "filenamePatterns", project.ProjectReferences.Select(x=> $"/{x}") },
                                                    { "scope", new [] {scope } }
                                                });
                    policy.Settings = settings;

                    policyClient.UpdatePolicyConfigurationAsync(policy, Global.Config.AzureDevops.ProjectName, policy.Id).Wait();

                    ColorConsole.WriteEmbeddedColorLine($"   - Branch Policy: [DarkGreen]{buildFile.BuildName}[/DarkGreen] - [yellow]Policy Updated[/yellow]");
                }
            }
            catch (Exception ex)
            {
                ColorConsole.WriteEmbeddedColorLine($"Failed to create policy: [red]{buildFile.BuildName}[/red]");
                ColorConsole.WriteEmbeddedColorLine(ex.Message, ConsoleColor.Red);
            }
        }
    }

    public bool CreatBuildDefinition(SolutionFile solution, Pipeline buildFile, string name, List<string> projectReferences)
    {
        using var buildClient = GetClient<BuildHttpClient>();
        var build = _builds.FirstOrDefault(b => ((YamlProcess)b.Process).YamlFilename == buildFile.RelativePath.ForwardSlashes());
        if (build is null)
        {
            try
            {
                var definition = new BuildDefinition()
                {
                    Path = solution.PipelineDirectory,
                    Name = name,
                    Process = new YamlProcess()
                    {
                        YamlFilename = buildFile.RelativePath.ForwardSlashes(),

                    },
                    Repository = new BuildRepository()
                    {
                        Id = _repository.Id.ToString(),
                        Type = "TfsGit"
                    },
                    Queue = new AgentPoolQueue()
                    {
                        Id = _queues.First().Id
                    },

                    QueueStatus = DefinitionQueueStatus.Enabled,

                };
                var trigger = new ContinuousIntegrationTrigger();
                definition.Triggers.Add(new Trigger()
                {
                    BranchFilters = new List<string>() { $"+{_repository.DefaultBranch}" },
                    PathFilters = projectReferences.Select(x => $"+/{x}").ToList()
                });
                var json = JsonConvert.SerializeObject(definition);
                buildClient.CreateDefinitionAsync(definition, Global.Config.AzureDevops.ProjectName).Wait();
                return true;
            }
            catch (Exception ex)
            {
                ColorConsole.WriteEmbeddedColorLine($"Failed to create build pipeline: [red]{buildFile.RelativePath}[/red]");
                throw;
            }

        }
        else
        {
            return false;
        }
    }

}

public class Trigger : BuildTrigger
{
    public Trigger() : base(DefinitionTriggerType.ContinuousIntegration)
    {
    }
    [DataMember]
    public int SettingsSourceType { get; set; } = 2;
    [DataMember]
    public bool BatchChanges { get; set; }
    [DataMember]
    public int MaxConcurrentBuildsPerBranch { get; set; } = 1;
    [DataMember(EmitDefaultValue = false)]
    public List<string> BranchFilters { get; set; }

    [DataMember(EmitDefaultValue = false)]
    public List<string> PathFilters { get; set; } = new List<string>();
}