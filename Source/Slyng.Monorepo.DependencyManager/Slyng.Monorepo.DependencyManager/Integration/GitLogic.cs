using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slyng.Monorepo.DependencyManager.Integration
{
    internal class GitLogic
    {
        public string GetRootPath()
        {
            var p = Process.Start(
              new ProcessStartInfo("git", "rev-parse --show-toplevel")
              {
                  CreateNoWindow = true,
                  UseShellExecute = false,
                  RedirectStandardError = true,
                  RedirectStandardOutput = true,
                  WorkingDirectory = Environment.CurrentDirectory
              }
            );

            p.WaitForExit();
            string rootDirectory = p.StandardOutput.ReadToEnd().TrimEnd();
            string errorInfoIfAny = p.StandardError.ReadToEnd().TrimEnd();

            if (errorInfoIfAny.Length != 0)
            {
                Console.WriteLine($"error: {errorInfoIfAny}");
            }

            return rootDirectory.Replace("/", "\\");
        }
    }
}
