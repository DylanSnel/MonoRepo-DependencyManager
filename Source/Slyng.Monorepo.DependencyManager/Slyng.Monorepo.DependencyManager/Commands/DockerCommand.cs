using CommandLine;
using Slyng.Monorepo.DependencyManager.Commands.Interfaces;
using Slyng.Monorepo.DependencyManager.Helpers;
using Slyng.Monorepo.DependencyManager.Integration;
using Slyng.Monorepo.DependencyManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slyng.Monorepo.DependencyManager.Commands
{
    [Verb("Docker", HelpText = "Run updates for the docker cache dependencies")]
    public class DockerCommand: ICommand
    {
        public void Execute()
        {
            DockerLogic.UpdateDocker();
        }     
    }
}
