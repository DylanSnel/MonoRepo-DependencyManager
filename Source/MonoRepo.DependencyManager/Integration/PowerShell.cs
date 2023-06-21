using MonoRepo.DependencyManager.Helpers;
using System.Diagnostics;

namespace MonoRepo.DependencyManager.Integration;
internal static class PowerShell
{
    public static string Execute(string command, bool show = false)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = command,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = !show,
        };

        var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();

        process.WaitForExit();
        var output = process.StandardOutput.ReadToEnd().TrimEnd();
        var errorInfoIfAny = process.StandardError.ReadToEnd().TrimEnd();

        if (errorInfoIfAny.Length != 0)
        {
            ColorConsole.WriteEmbeddedColorLine($"[red]Command error Error::[/red] {errorInfoIfAny}");
            return errorInfoIfAny.Trim();
        }

        return output.Trim();
    }
}
