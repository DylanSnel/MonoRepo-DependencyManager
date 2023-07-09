using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.TeamFoundation.Policy.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;
using MonoRepo.DependencyManager.Helpers;
using MonoRepo.DependencyManager.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
                ResetConnection();
            }
            return _connection;
        }
    }

    public void ResetConnection()
    {
        if (!string.IsNullOrEmpty(Global.PersonalAccessToken) && !string.IsNullOrEmpty(Global.Config.AzureDevops.AzureDevopsUrl))
        {
            var creds = new VssBasicCredential(string.Empty, Global.PersonalAccessToken);
            _connection = new VssConnection(new Uri(Global.Config.AzureDevops.AzureDevopsUrl), creds);
        }
        if (!string.IsNullOrEmpty(Global.DevopsAccessToken) && !string.IsNullOrEmpty(Global.Config.AzureDevops.AzureDevopsUrl))
        {
            var creds = new VssOAuthAccessTokenCredential(Global.DevopsAccessToken);
            _connection = new VssConnection(new Uri(Global.Config.AzureDevops.AzureDevopsUrl), creds);
        }
    }


    private VssConnection _connection;

    private GitRepository _repository;
    private List<BuildDefinition> _builds;
    private List<PolicyConfiguration> _policies;
    private List<TaskAgentQueue> _queues;
    private readonly Guid _buildPolicyType = new("0609b952-1397-4640-95ec-e00a01b2c241");

    public AzureDevopsClient(bool init = true)
    {
        if (init)
        {
            Init();
        }
    }

    public void Init()
    {
        _repository = GetRepository();
        _builds = GetAllBuilds();
        _policies = GetAllPolicies();
        _queues = GetAgentPool();
    }
    public T GetClient<T>() where T : VssHttpClientBase
    {
        ResetConnection();
        return Connection.GetClient<T>();
    }
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

    public (bool, bool) CreateOrUpdatePolicy(ProjectFile project, Pipeline buildFile, string branch, bool required)
    {
        var build = _builds.FirstOrDefault(b => ((YamlProcess)b.Process).YamlFilename == buildFile.RelativePath.ForwardSlashes());
        if (build != null)
        {
            try
            {
                using var policyClient = GetClient<PolicyHttpClient>();
                var policy = _policies.FirstOrDefault(p => p.Settings.Value<int>("buildDefinitionId") == build.Id && p.Settings.SelectToken("$..refName").ToString() == branch);
                if (policy == null)
                {
                    var scope = new Dictionary<string, object>()
                                                {
                                                    {"refName", branch},
                                                    {"matchKind", "Exact"},
                                                    {"repositoryId", _repository.Id},
                                                };

                    var settings = JObject.FromObject(new Dictionary<string, object>()
                                                {
                                                    { "buildDefinitionId", build.Id },
                                                    { "queueOnSourceUpdateOnly", true },
                                                    { "manualQueueOnly", false },
                                                    { "displayName", build.Name},
                                                    { "validDuration", 720.0d },
                                                    { "filenamePatterns", project.BuildProjectReferences.Select(x=> $"/{x}") },
                                                    { "scope", new [] {scope } }
                                                });

                    policy = new PolicyConfiguration()
                    {
                        IsEnabled = true,
                        Type = new PolicyTypeRef()
                        {
                            Id = _buildPolicyType
                        },
                        IsBlocking = required,
                        Settings = settings,
                    };
                    policyClient.CreatePolicyConfigurationAsync(policy, Global.Config.AzureDevops.ProjectName).GetAwaiter().GetResult();
                    return (true, policy.IsBlocking);
                }
                else
                {
                    var scope = new Dictionary<string, object>()
                                                {
                                                    {"refName", branch},
                                                    {"matchKind", "Exact"},
                                                    {"repositoryId", _repository.Id},
                                                };
                    var settings = JObject.FromObject(new Dictionary<string, object>()
                                                {
                                                    { "buildDefinitionId", build.Id },
                                                    { "queueOnSourceUpdateOnly", true },
                                                    { "manualQueueOnly", false },
                                                    { "displayName", build.Name},
                                                    { "validDuration", 720.0d },
                                                    { "filenamePatterns", project.BuildProjectReferences.Select(x=> $"/{x}") },
                                                    { "scope", new [] {scope } }
                                                });
                    policy.Settings = settings;
                    if (!policy.IsBlocking)
                    {
                        policy.IsBlocking = required;
                    }

                    policyClient.UpdatePolicyConfigurationAsync(policy, Global.Config.AzureDevops.ProjectName, policy.Id).GetAwaiter().GetResult();
                    _policies = GetAllPolicies();
                    return (false, policy.IsBlocking);
                }
            }
            catch (Exception ex)
            {
                ColorConsole.WriteEmbeddedColorLine($"Failed to create policy: [red]{buildFile.BuildName}[/red]");
                throw;
            }
        }
        else
        {
            throw new Exception($"Build definition not found for: {buildFile.BuildName}");
        }
    }

    public bool CreateOrUpdateBuildDefinition(SolutionFile solution, Pipeline buildFile, string name, List<string> projectReferences)
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
                _builds = GetAllBuilds();
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
            if (build.Name != name)
            {
                build.Name = name;
                buildClient.UpdateDefinitionAsync(build, Global.Config.AzureDevops.ProjectName, build.Id).Wait();
                _builds = GetAllBuilds();
            }
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
