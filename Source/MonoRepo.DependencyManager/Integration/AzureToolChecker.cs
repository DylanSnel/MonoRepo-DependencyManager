using Newtonsoft.Json.Linq;

namespace MonoRepo.DependencyManager.Integration;
internal static class AzureToolChecker
{
    public static bool IsAzureCliInstalled()
    {
        var azureCliVersionCommand = "az --version";
        var output = PowerShell.Execute(azureCliVersionCommand);

        // If Azure CLI is installed, the command will return the version number.
        // If not, the command will return an error.
        return !output.Contains("az : The term 'az' is not recognized");
    }

    public static JObject GetAzureDevopsAccesToken()
    {
        var azureCliVersionCommand = "az account get-access-token";
        var output = PowerShell.Execute(azureCliVersionCommand);

        return JObject.Parse(output);
    }

    public static bool IsAzureDevOpsCliInstalled()
    {
        var azureDevOpsCliVersionCommand = "az extension show --name azure-devops";
        var output = PowerShell.Execute(azureDevOpsCliVersionCommand, false);

        // If Azure DevOps CLI is installed, the command will return help information.
        // If not, the command will return an error.
        return !output.Contains("'devops' is misspelled or not recognized by the system.");
    }

    public static bool IsLoggedInAzure()
    {
        var azureLoginStatusCommand = "az account show";
        var output = PowerShell.Execute(azureLoginStatusCommand);

        // If the user is logged into Azure, the command will return account information.
        // If not, the command will return an error.
        return !output.Contains("Please run 'az login' to setup account");
    }

    public static string InstallAzureCli()
    {
        var azureCliInstallCommand = "Invoke-WebRequest -Uri https://aka.ms/installazurecliwindows -OutFile .\\AzureCLI.msi; Start-Process msiexec.exe -Wait -ArgumentList '/I AzureCLI.msi /quiet'; rm .\\AzureCLI.msi";
        var output = PowerShell.Execute(azureCliInstallCommand, true);

        return output;
    }

    public static string InstallAzureDevOpsCli()
    {
        var azureDevOpsCliInstallCommand = "az extension add --name azure-devops";
        var output = PowerShell.Execute(azureDevOpsCliInstallCommand, true);

        return output;
    }
}
