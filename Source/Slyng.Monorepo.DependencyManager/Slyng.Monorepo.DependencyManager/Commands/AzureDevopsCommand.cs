using CommandLine;
using Slyng.Monorepo.DependencyManager.Commands.Interfaces;
using Slyng.Monorepo.DependencyManager.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slyng.Monorepo.DependencyManager.Commands
{
    [Verb("Update-Devops", HelpText = "Add configuration file to the root repo")]
    public class AzureDevopsCommand : DevopsCommand, ICommand
    {
        public void Execute()
        {
            if (Global.Config.AzureDevops.Enabled)
            {
                AzureDevopsClient client = new();
                client.Update();
            }
            Console.WriteLine("Update command");
        }
    }
}
