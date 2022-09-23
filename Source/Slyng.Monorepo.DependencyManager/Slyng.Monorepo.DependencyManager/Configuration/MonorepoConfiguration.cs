using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slyng.Monorepo.DependencyManager.Configuration
{
    public class MonorepoConfiguration
    {
        public bool EnableDocker { get; set; }
        public string DockerFileExtention { get; set; } = "DockerFile";
        public DevopsConfiguration AzureDevops { get; set; } = new DevopsConfiguration();

    }
}
