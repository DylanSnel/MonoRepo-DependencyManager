using MonoRepo.DependencyManager.Helpers;
using MonoRepo.DependencyManager.Models;
using System.Text;

namespace MonoRepo.DependencyManager.Integration;

public class DockerLogic
{
    public static bool ReplaceDockerReferences(ProjectFile projectFile, string dockerFile)
    {
        var replaceMents = BuildReplacement(projectFile);

        var text = File.ReadAllText(dockerFile);
        var explodedText = text.Split(new[] { "#DependencyToolGeneratedCode", "#End" }, StringSplitOptions.None);
        if (explodedText.Length != 3)
        {
            return false;
        }
        explodedText[1] = replaceMents;

        var newText = string.Join("", new string[] { explodedText[0], "#DependencyToolGeneratedCode", "\r\n", explodedText[1], "\r\n", "#End", explodedText[2] });
        File.WriteAllText(dockerFile, newText);
        return true;
    }

    private static string BuildReplacement(ProjectFile projectFile)
    {
        var replacement = new StringBuilder();
        replacement.AppendLine($"WORKDIR /src");
        foreach (var solution in Global.Solutions)
        {
            foreach (var project in solution.Projects)
            {
                replacement.AppendLine($"COPY [\"{project.RelativePath.ForwardSlashes()}\", \"{project.RelativeDirectory.ForwardSlashes()}\"]");
            }
        }
        foreach (var buildprops in Global.BuildProps)
        {
            replacement.AppendLine($"COPY [\"{buildprops.ForwardSlashes()}\", \"{buildprops.ForwardSlashes()}\"]");
        }
        replacement.AppendLine($"RUN dotnet restore \"{projectFile.RelativePath.ForwardSlashes()}\"");
        replacement.AppendLine($"COPY . .");
        replacement.AppendLine($"WORKDIR \"/src/{projectFile.RelativeDirectory.ForwardSlashes()}\"");
        return replacement.ToString();
    }
}
