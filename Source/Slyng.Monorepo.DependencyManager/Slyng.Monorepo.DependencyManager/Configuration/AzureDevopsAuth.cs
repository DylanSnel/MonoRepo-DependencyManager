using Newtonsoft.Json;
using Slyng.Monorepo.DependencyManager.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slyng.Monorepo.DependencyManager.Configuration
{
    public class AzureDevopsAuth
    {
        public string CollectionUri { get; set; }
        public string ProjectName { get; set; }
        public string RepoName { get; set; }

        [JsonProperty("AccessToken")]
        private string _accessToken;
        public bool UsePassword { get; set; }
        [JsonIgnore]
        public string Pat
        {
            get
            {
                if (UsePassword)
                {
                    if(Global.Password == "")
                    {
                        throw new AccessViolationException("Password is required");
                    }
                    return _accessToken.Decrypt(Global.Password);
                }
                else
                {
                    return _accessToken;
                }
            }
            set
            {
                if (UsePassword)
                {
                   _accessToken = value.Encrypt(Global.Password);
                }
                else
                {
                    _accessToken= value;
                }

            }
        }

    }
}
