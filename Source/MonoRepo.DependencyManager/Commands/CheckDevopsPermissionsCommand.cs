using CommandLine;
using MonoRepo.DependencyManager.Commands.Interfaces;
using MonoRepo.DependencyManager.Helpers;
using MonoRepo.DependencyManager.Integration;

namespace MonoRepo.DependencyManager.Commands;

[Verb("check-permissions", HelpText = "Check if you are ready to run this tool")]
internal class CheckDevopsPermissionsCommand : ICommand
{
    public void Execute()
    {
        ColorConsole.WriteEmbeddedColorLine("[yellow]Checking if commandline tools are configured correctly[/yellow]");
        ColorConsole.WriteEmbeddedColorLine("");

        ColorConsole.WriteEmbeddedColor("Checking for [darkblue]Azure Cli[/darkblue]: ");
        if (AzureToolChecker.IsAzureCliInstalled())
        {
            ColorConsole.WriteEmbeddedColorLine("[green]installed[/green]");
        }
        else
        {
            ColorConsole.WriteEmbeddedColorLine("[red]not installed[/red]");
            var install = Cli.Confirm("Do you want to install [blue]Azure Cli?[/blue]");
            if (install)
            {
                AzureToolChecker.InstallAzureCli();
                ColorConsole.WriteEmbeddedColor("Checking for [darkblue]Azure Cli[/darkblue]: ");
                if (AzureToolChecker.IsAzureCliInstalled())
                {
                    ColorConsole.WriteEmbeddedColorLine("[green]installed[/green]");
                }
                else
                {
                    throw new Exception("[red]Azure Devops Cli can not be installed[/red]");
                }
            }
            else
            {
                ColorConsole.WriteEmbeddedColorLine("[red]Cancel[/red]");
                throw new Exception("Azure Cli is not installed");
            }
        }

        ColorConsole.WriteEmbeddedColor("Checking for [blue]Azure Devops Cli[/blue]: ");
        if (AzureToolChecker.IsAzureDevOpsCliInstalled())
        {
            ColorConsole.WriteEmbeddedColorLine("[green]installed[/green]");
        }
        else
        {
            ColorConsole.WriteEmbeddedColorLine("[red]Azure Devops Cli is not installed[/red]");
            var install = Cli.Confirm("Do you want to install Azure Devops Cli?");
            if (install)
            {
                AzureToolChecker.InstallAzureDevOpsCli();
                ColorConsole.WriteEmbeddedColor("Checking for [blue]Azure Devops Cli[/blue]: ");
                if (AzureToolChecker.IsAzureDevOpsCliInstalled())
                {
                    ColorConsole.WriteEmbeddedColorLine("[green]installed[/green]");
                }
                else
                {
                    throw new Exception("[red]Azure Devops Cli can not be installed[/red]");
                }
            }
            else
            {
                ColorConsole.WriteEmbeddedColorLine("[red]Cancel[/red]");
                throw new Exception("Azure Devops Cli is not installed");
            }

        }

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
}
