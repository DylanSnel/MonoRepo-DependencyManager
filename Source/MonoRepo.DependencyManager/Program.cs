using CommandLine;
using MonoRepo.DependencyManager.Commands;
using MonoRepo.DependencyManager.Configuration;
using MonoRepo.DependencyManager.Helpers;
using MonoRepo.DependencyManager.Integration;
using MonoRepo.DependencyManager.Models;
using Newtonsoft.Json;

namespace MonoRepo.DependencyManager;

public class Program
{
    static void Main(string[] args)
    {

#if DEBUG
        while (true)
        {
            args = Console.ReadLine().Split(' ');
            try
            {
#endif
                if (args.Contains("--pipeline") || args.Contains("-p"))
                {
                    Global.IsPipeline = true;
                    args = args.Except(new[] { "--pipeline", "-p" }).ToArray();
                }

                Initialize();

                Parser.Default.ParseArguments<InitCommand, UpdateCommand, CheckDevopsPermissionsCommand, ProjectTreeCommand>(args)
                    .WithParsed<InitCommand>(t => t.Execute())
                    .WithParsed<UpdateCommand>(t => t.Execute())
                    .WithParsed<ProjectTreeCommand>(t => t.Execute())
                    .WithParsed<CheckDevopsPermissionsCommand>(t => t.Execute());

#if DEBUG
            }
            catch (Exception ex)
            {
                ColorConsole.WriteEmbeddedColorLine(ex.Message);
            }
            ColorConsole.WriteEmbeddedColorLine("=======================================================");
        }
#endif
    }

    public static void Initialize()
    {
        Global.RootPath = GitLogic.GetRootPath();
        Global.CurrentBranch = GitLogic.GetCurrentBranch();
        ColorConsole.WriteEmbeddedColorLine($"Repository Root: [Green]{Global.RootPath}[/Green]");
        ColorConsole.WriteEmbeddedColorLine($"Current branch: [Blue]{Global.CurrentBranch}[/Blue]");
        Global.Solutions = FileHelper.GetFilesByType(Global.RootPath, "*.sln").Select(csproj => new SolutionFile(csproj)).ToList();
        Global.BuildProps = FileHelper.GetFilesByType(Global.RootPath, "*.Build.props").Select(x => x.Replace(Global.RootPath, "").TrimStart('\\')).ToList();
        ColorConsole.WriteEmbeddedColorLine($"Found [magenta]{Global.Solutions.Count}[/magenta] Solutions");
        ColorConsole.WriteEmbeddedColorLine($"Found [yellow]{Global.Solutions.SelectMany(x => x.Projects).SelectMany(x => x.BuildProjectReferences).Distinct().Count()}[/yellow] Projects");

        ColorConsole.WriteEmbeddedColorLine("");
    }

    public static bool CheckForConfiguration()
    {
        if (File.Exists(Global.ConfigFilePath))
        {
            Global.Config = JsonConvert.DeserializeObject<MonorepoConfiguration>(File.ReadAllText(Global.ConfigFilePath));
            Global.Solutions = FileHelper.GetFilesByType(Global.RootPath, "*.sln").Select(csproj => new SolutionFile(csproj)).ToList();
            ColorConsole.WriteEmbeddedColorLine($"Congfiguration: [Green]Found[/Green]");
            if (Global.Config.AzureDevops.Enabled && string.IsNullOrEmpty(Global.PersonalAccessToken))
            {
                var accessToken = AzureToolChecker.GetAzureDevopsAccesToken();
                if (accessToken.SelectToken("$..accessToken") != null)
                {
                    ColorConsole.WriteEmbeddedColorLine("Azure devops access token: [blue]Received token[/blue]");
                    Global.DevopsAccessToken = accessToken.SelectToken("$..accessToken").ToString();
                    return true;
                }
                else
                {
                    ColorConsole.WriteEmbeddedColorLine("Azure devops access token: [red]No token recieved[/red]");
                    if (Global.IsPipeline)
                    {
                        throw new Exception("Could not retrieve an access token");
                    }
                    else
                    {
                        ColorConsole.WriteEmbeddedColorLine($"Please run \"check\" command first");
                        return false;
                    }
                }
            }
            return true;
        }
        else
        {
            ColorConsole.WriteEmbeddedColorLine($"Congfiguration: [red]Not found[/red]");
            if (Global.IsPipeline)
            {
                throw new Exception("Configuration file is not found");
            }
            else
            {
                ColorConsole.WriteEmbeddedColorLine($"Please run \"init\" command first");
                return false;
            }
        }
    }
}

public static class Global
{
    public const string ConfigFileName = ".monorepo-config";
    public static string ConfigFilePath => Path.Combine(RootPath, ConfigFileName);

    public static bool IsPipeline { get; set; } = false;

    public static string DevopsAccessToken { get; set; } = "";

    public static string RootPath { get; set; } = "";
    public static MonorepoConfiguration Config { get; set; } = new();
    public static List<SolutionFile> Solutions { get; set; } = new List<SolutionFile>();
    public static List<string> BuildProps { get; set; } = new List<string>();
    public static string CurrentBranch { get; internal set; }
    public static string PersonalAccessToken { get; internal set; }
}
