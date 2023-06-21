using MonoRepo.DependencyManager.Helpers;
using System;
using System.Diagnostics;

namespace MonoRepo.DependencyManager.Integration;

internal class GitLogic
{
    public static string GetRootPath()
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
        var rootDirectory = p.StandardOutput.ReadToEnd().TrimEnd();
        var errorInfoIfAny = p.StandardError.ReadToEnd().TrimEnd();

        if (errorInfoIfAny.Length != 0)
        {
            ColorConsole.WriteEmbeddedColorLine($"Git Error:: {errorInfoIfAny}");
        }

        return rootDirectory.Replace("/", "\\");
    }

    public static string GetCurrentBranch()
    {
        var p = Process.Start(
          new ProcessStartInfo("git", "rev-parse --abbrev-ref HEAD")
          {
              CreateNoWindow = true,
              UseShellExecute = false,
              RedirectStandardError = true,
              RedirectStandardOutput = true,
              WorkingDirectory = Environment.CurrentDirectory
          }
        );

        p.WaitForExit();
        var currentBranch = p.StandardOutput.ReadToEnd().TrimEnd();
        var errorInfoIfAny = p.StandardError.ReadToEnd().TrimEnd();

        if (errorInfoIfAny.Length != 0)
        {
            ColorConsole.WriteEmbeddedColorLine($"Git Error:: {errorInfoIfAny}");
        }

        return currentBranch;
    }
}
