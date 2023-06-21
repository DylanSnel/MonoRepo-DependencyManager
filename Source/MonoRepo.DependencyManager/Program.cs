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
#endif
            try
            {
                Console.WriteLine(string.Join(',', args));
                Initialize();

                Parser.Default.ParseArguments<InitCommand, UpdateCommand, CheckDevopsPermissionsCommand, ProjectTreeCommand>(args)
                    .WithParsed<InitCommand>(t => t.Execute())
                    .WithParsed<UpdateCommand>(t => t.Execute())
                    .WithParsed<ProjectTreeCommand>(t => t.Execute())
                    .WithParsed<CheckDevopsPermissionsCommand>(t => t.Execute());
            }
            catch (Exception ex)
            {
                ColorConsole.WriteEmbeddedColorLine(ex.Message);
            }
#if DEBUG
            ColorConsole.WriteEmbeddedColorLine("=======================================================");
        }
#endif

    }

    private static void Initialize()
    {
        Global.RootPath = GitLogic.GetRootPath();
        Global.CurrentBranch = GitLogic.GetCurrentBranch();
        ColorConsole.WriteEmbeddedColorLine($"Using [Green]{Global.RootPath}[/Green] as the base directory of the repository");
        ColorConsole.WriteEmbeddedColorLine($"Currentbranch [Blue]{Global.CurrentBranch}[/Blue] as the current branch");
        ColorConsole.WriteEmbeddedColorLine("");
        if (File.Exists(Global.ConfigFilePath))
        {
            Global.Config = JsonConvert.DeserializeObject<MonorepoConfiguration>(File.ReadAllText(Global.ConfigFilePath));
            ColorConsole.WriteEmbeddedColorLine($"Configuration Found");
            ColorConsole.WriteEmbeddedColorLine("");
            ColorConsole.WriteEmbeddedColor("Checking if we can get an [cyan]access token[/cyan]: ");
            var accessToken = AzureToolChecker.GetAzureDevopsAccesToken();
            if (accessToken.SelectToken("$..accessToken") != null)
            {
                ColorConsole.WriteEmbeddedColorLine("[yellow]Token found[/yellow]");
                Global.DevopsAccessToken = accessToken.SelectToken("$..accessToken").ToString();
            }
            else
            {
                ColorConsole.WriteEmbeddedColorLine("[red]Access token is not found[/red]");
                throw new Exception("Access token is not found");
            }
        }

        Global.Solutions = FileHelper.GetFilesByType(Global.RootPath, "*.sln").Select(csproj => new SolutionFile(csproj)).ToList();
        ColorConsole.WriteEmbeddedColorLine("");
    }

}

public static class Global
{
    public const string ConfigFileName = ".monorepo-config";
    public static string ConfigFilePath => Path.Combine(RootPath, ConfigFileName);


    public static string DevopsAccessToken { get; set; } = "";

    public static string RootPath { get; set; } = "";
    public static MonorepoConfiguration Config { get; set; } = new();
    public static List<SolutionFile> Solutions { get; set; } = new List<SolutionFile>();
    public static string CurrentBranch { get; internal set; }
}
