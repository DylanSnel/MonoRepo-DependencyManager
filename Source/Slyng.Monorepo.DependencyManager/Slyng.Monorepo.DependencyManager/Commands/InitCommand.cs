using CommandLine;
using Newtonsoft.Json;
using Slyng.Monorepo.DependencyManager.Commands.Interfaces;
using Slyng.Monorepo.DependencyManager.Configuration;
using Slyng.Monorepo.DependencyManager.Integration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slyng.Monorepo.DependencyManager.Commands
{
    [Verb("Init", HelpText = "Initialize monorepo configuration")]
    internal class InitCommand : ICommand
    {
        [Option("overwrite", Required = false, HelpText = "Overwrite existing configuration")]
        public bool OverWrite { get; set; }
        [Option("azure-devops", Required = false, HelpText = "Enable Azure Devops")]
        public bool AzureDevops { get; set; }
        [Option("docker", Required = false, HelpText = "Enable Docker Caching")]
        public bool Docker { get; set; }

        public void Execute()
        {
            MonorepoConfiguration config = new()
            {
                EnableDocker = Docker
            };
            var filePath = Path.Combine(Global.RootPath, Global.ConfigFileName);
            if (File.Exists(filePath) && !OverWrite)
            {
                Console.WriteLine("Configuration file already exists. Use --overwrite to overwrite");
                return;
            }

            if (AzureDevops)
            {
                config.AzureDevops = AzureDevopsClient.InitConfiguration();
            }

            File.WriteAllText(Path.Combine(Global.RootPath, Global.ConfigFileName), JsonConvert.SerializeObject(config, Formatting.Indented));
            Console.WriteLine("Configuration file created");

            if (AzureDevops)
            {
                AzureDevopsClient.InitAuthentication(OverWrite);
            }
        }
    }
}
