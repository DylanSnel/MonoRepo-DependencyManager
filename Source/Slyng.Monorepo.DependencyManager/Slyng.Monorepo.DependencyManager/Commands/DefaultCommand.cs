using CommandLine;

namespace Slyng.Monorepo.DependencyManager.Commands
{
    public class DevopsCommand
    {
        [Option('p', "password", Required = false, HelpText = "Give a password to decrypt the azure devops access token")]
        public string Password
        {
            get
            {
                return Global.Password;
            }
            set
            {
                Global.Password = value;
            }
        }

        [Option('t', "access-token", Required = false, HelpText = "The azure devops access token")]
        public string Token
        {
            get
            {
                return Global.AzureDevopsConfig.Pat;
            }
            set
            {
                Global.AzureDevopsConfig.UsePassword = false;
                Global.AzureDevopsConfig.Pat = value;
            }
        }
    }
}
