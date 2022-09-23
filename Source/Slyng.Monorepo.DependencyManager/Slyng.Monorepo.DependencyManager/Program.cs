using CommandLine;
using Newtonsoft.Json;
using Slyng.Monorepo.DependencyManager.Commands;
using Slyng.Monorepo.DependencyManager.Configuration;
using Slyng.Monorepo.DependencyManager.Helpers;
using Slyng.Monorepo.DependencyManager.Integration;
using Slyng.Monorepo.DependencyManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Slyng.Monorepo.DependencyManager
{
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
                    Initialize();

                    Parser.Default.ParseArguments<InitCommand, UpdateCommand, DockerCommand, AzureDevopsCommand>(args)
                        .WithParsed<InitCommand>(t => t.Execute())
                        .WithParsed<UpdateCommand>(t => t.Execute())
                        .WithParsed<DockerCommand>(t => t.Execute())
                        .WithParsed<AzureDevopsCommand>(t => t.Execute());

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
#if DEBUG
                Console.WriteLine("=======================================================");
            }
#endif

        }

        private static void Initialize()
        {
            GitLogic git = new();

            Global.RootPath = git.GetRootPath();

            Console.WriteLine($"Using {Global.RootPath} as the base directory of the repository");
            Console.WriteLine();
            if (File.Exists(Global.ConfigFilePath))
            {
                Global.Config =JsonConvert.DeserializeObject<MonorepoConfiguration>( File.ReadAllText(Global.ConfigFilePath));
                Console.WriteLine($"Configuration Found");
                Console.WriteLine();
            }

            Global.Solutions = FileHelper.GetFilesByType(Global.RootPath, "*.sln").Select(csproj => new SolutionFile(csproj)).ToList();
            Console.WriteLine();

            if (Global.Config.AzureDevops.Enabled && File.Exists(Global.DevopsFilePath))
            {
                Global.AzureDevopsConfig = JsonConvert.DeserializeObject<AzureDevopsAuth>(File.ReadAllText(Global.DevopsFilePath));
                Console.WriteLine($"Devops Configuration Found");
                Console.WriteLine();
            }
            Console.WriteLine();
        }

    }

    public static class Global
    {
        public const string ConfigFileName = ".monorepo-config";
        public static string ConfigFilePath => Path.Combine(Global.RootPath, Global.ConfigFileName);
        public const string DevopsFileName = ".monorepo-devops-auth";
        public static string DevopsFilePath => Path.Combine(Global.RootPath, Global.DevopsFileName);


        public static string RootPath { get; set; } = "";
        public static string Password {get; set;} = "";
        public static AzureDevopsAuth AzureDevopsConfig { get; set; } = new AzureDevopsAuth();
        public static MonorepoConfiguration Config { get; set; } = new MonorepoConfiguration();
        public static List<ProjectFile> Projects { get; set; } = new List<ProjectFile>();
        public static List<SolutionFile> Solutions { get; set; } = new List<SolutionFile>();
    }
}
