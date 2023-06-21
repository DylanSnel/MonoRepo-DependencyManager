using CommandLine;
using MonoRepo.DependencyManager.Commands.Interfaces;
using MonoRepo.DependencyManager.Integration;

namespace MonoRepo.DependencyManager.Commands;

[Verb("Docker", HelpText = "Run updates for the docker cache dependencies")]
public class DockerCommand : ICommand
{
    public void Execute()
    {
        DockerLogic.UpdateDocker();
    }
}
