using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.TeamFoundation.Policy.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Slyng.Monorepo.DependencyManager.Configuration;
using Slyng.Monorepo.DependencyManager.Helpers;
using Slyng.Monorepo.DependencyManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Slyng.Monorepo.DependencyManager.Integration
{
    internal class AzureDevopsClient
    {
        private readonly AzureDevopsAuth _auth = Global.AzureDevopsConfig;
        private readonly DevopsConfiguration _configuration = Global.Config.AzureDevops;
        private VssConnection _connection;

        private readonly GitRepository _repository;
        private List<BuildDefinition> _builds;
        private List<PolicyConfiguration> _policies;
        private List<TaskAgentQueue> _queues;
        private readonly Guid _buildPolicyType = new Guid("0609b952-1397-4640-95ec-e00a01b2c241");

        public AzureDevopsClient()
        {
            var creds = new VssBasicCredential(string.Empty, _auth.Pat);
            _connection = new VssConnection(new Uri(_auth.CollectionUri), creds);

            _repository = GetRepository();
            _builds = GetAllBuilds();
            _policies = GetAllPolicies();
            _queues = GetAgentPool();
        }

        public static void InitAuthentication(bool overWrite)
        {
            AzureDevopsAuth auth = new()
            {
                CollectionUri = Cli.AskFor<string>("Azure Devops Uri", Global.AzureDevopsConfig.CollectionUri),
                ProjectName = Cli.AskFor<string>("Project name", Global.AzureDevopsConfig.ProjectName),
                RepoName = Cli.AskFor<string>("Repository", Global.AzureDevopsConfig.RepoName),
                UsePassword = Cli.Confirm("Use password for Personal Access Token"),
            };

            if (auth.UsePassword)
            {
                Global.Password = Cli.AskFor<string>("Password: ");
            }
            auth.Pat = Cli.AskFor<string>("Personal Access Token: ");

            var filePath = Global.DevopsFilePath;
            if (File.Exists(filePath) && !overWrite)
            {
                Console.WriteLine("Authentication file for Azure Devops already exists. Use --overwrite to overwrite");
                return;
            }

            Global.AzureDevopsConfig = auth;

            Console.WriteLine($"Trying to access the repository: {auth.CollectionUri}");
            AzureDevopsClient adClient = new();
            Console.WriteLine($"Success");

            File.WriteAllText(filePath, JsonConvert.SerializeObject(auth, Formatting.Indented));
            Console.WriteLine("Authentication file for Azure Devops created");
        }

        public static DevopsConfiguration InitConfiguration()
        {
            DevopsConfiguration config = new()
            {
                Enabled = true,
                UpdateBuildPipelinesReferences = Cli.Confirm("Automatically update build pipeline references?"),
                UsePrPipelinesAsBranchPolicies = Cli.Confirm("Automatically set PR builds as build policy?"),
                AutoImportBuildPipelines = Cli.Confirm("Automatically import build pipelines?"),
                AutoImportPrPipelines = Cli.Confirm("Automatically import PR pipelines?"),
            };

            return config;
        }

        private T GetClient<T>() where T: VssHttpClientBase
        {
            var creds = new VssBasicCredential(string.Empty, _auth.Pat);
            _connection = new VssConnection(new Uri(_auth.CollectionUri), creds);
            return _connection.GetClient<T>();
        }


        public GitRepository GetRepository()
        {
            // Get a GitHttpClient to talk to the Git endpoints
            using var gitClient = _connection.GetClient<GitHttpClient>();
            // Get data about a specific repository
            return gitClient.GetRepositoryAsync(_auth.ProjectName, _auth.RepoName).Result;
        }

        public List<BuildDefinition> GetAllBuilds()
        {
            // Get a GitHttpClient to talk to the Git endpoints
            using var buildClient = GetClient<BuildHttpClient>();

            // Get data about a specific repository
            return buildClient.GetFullDefinitionsAsync(project: _auth.ProjectName, repositoryId: _repository.Id.ToString(), repositoryType: "TfsGit").Result;
            //return buildClient.GetFullDefinitionsAsync(project: _auth.ProjectName).Result;
        }
        
        public List<TaskAgentQueue> GetAgentPool()
        {
            // Get a GitHttpClient to talk to the Git endpoints
            using var buildClient = GetClient<TaskAgentHttpClient>();
            return buildClient.GetAgentQueuesByNamesAsync(project: _auth.ProjectName, new List<string> { _configuration.AgentPoolName }).Result;
          
        }

        public List<PolicyConfiguration> GetAllPolicies()
        {
            // Get a GitHttpClient to talk to the Git endpoints
            using var policyClient = GetClient<PolicyHttpClient>();
            return policyClient.GetPolicyConfigurationsAsync(project: _auth.ProjectName, null, _buildPolicyType).Result;

        }

        public void Update()
        {

            Console.WriteLine();
            ColorConsole.WriteEmbeddedColorLine($"[green]Updating Azure Devops[/green]");
            foreach (var solution in Global.Solutions)
            {
                ColorConsole.WriteEmbeddedColorLine($"Solution: [blue]{solution.SolutionFileName}[/blue]");
                foreach (var project in solution.Projects)
                {
                    ColorConsole.WriteEmbeddedColorLine($" - Project: [yellow]{project.ProjectFileName}[/yellow]");
                    try
                    {                   
                        if (_configuration.UpdateBuildPipelinesReferences)
                        {
                            if (project.BuildPipelines.Any())
                            {
                                project.UpdateBuildPaths();
                                ColorConsole.WriteEmbeddedColorLine($"   - [darkyellow]Updating BuildPaths[/darkyellow] - [green]Complete[/green]");
                            }                           
                        }

                        if (_configuration.AutoImportBuildPipelines)
                        {
                            foreach (var buildFile in project.BuildPipelines)
                            {
                                CreatBuildDefinition(solution, buildFile, $"{buildFile.BuildName} - Build", project.ProjectReferences);
                            }
                        }
                        
                        if (_configuration.AutoImportPrPipelines)
                        {
                            foreach (var buildFile in project.PrPipelines)
                            {
                                CreatBuildDefinition(solution, buildFile, $"{buildFile.BuildName} - PullRequest", project.ProjectReferences);
                            }
                        }

                        _builds = GetAllBuilds();

                        if (_configuration.UsePrPipelinesAsBranchPolicies)
                        {
                            foreach (var buildFile in project.PrPipelines)
                            {
                                CreateNewPolicy(project, buildFile);
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        ColorConsole.WriteEmbeddedColorLine($"Error: [red]{ex.Message}[/red]");
                    }
                }
            }
        }

        private void CreateNewPolicy(ProjectFile project, Pipeline buildFile)
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
                        policyClient.CreatePolicyConfigurationAsync(policy, _auth.ProjectName).Wait();
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
       
                        policyClient.UpdatePolicyConfigurationAsync(policy, _auth.ProjectName, policy.Id).Wait();

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

        private void CreatBuildDefinition(SolutionFile solution, Pipeline buildFile, string name, List<string> projectReferences)
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
                    buildClient.CreateDefinitionAsync(definition, _auth.ProjectName).Wait();
                    ColorConsole.WriteEmbeddedColorLine($"   - Add Build: [cyan]{name}[/cyan] - [green]Complete[/green]");
                }
                catch (Exception ex)
                {
                    ColorConsole.WriteEmbeddedColorLine($"Failed to create build pipeline: [red]{buildFile.RelativePath}[/red]");
                    ColorConsole.WriteEmbeddedColorLine(ex.Message, ConsoleColor.Red);
                }


            }
            else
            {
                ColorConsole.WriteEmbeddedColorLine($"   - Add Build: [cyan]{name}[/cyan] - Already imported");
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
}
