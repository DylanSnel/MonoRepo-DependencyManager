using System.Text;
using YamlDotNet.RepresentationModel;

namespace MonoRepo.DependencyManager.Models;

public class Pipeline
{

    public Pipeline(string path, string projectDirectory)
    {
        FullPath = path;
        ProjectDirectory = projectDirectory;
    }
    public string FullPath { get; set; }


    public string ProjectDirectory { get; }

    public string FileName
    {
        get
        {
            return Path.GetFileName(FullPath);
        }
    }


    public string RelativePath
    {
        get
        {
            return FullPath.Replace(Global.RootPath, "").TrimStart('\\');
        }
    }

    public string BuildName
    {
        get
        {
            return Path.GetFileName(FullPath).Replace(Global.Config.BuildPipelinesFileExtension.Replace("*", ""), "").Replace(Global.Config.PolicyPipelinesFileExtension.Replace("*", ""), "");
        }
    }

    public string ExtensionName
    {
        get
        {
            return Path.GetFileNameWithoutExtension(FullPath).Split(".")[1] ?? "";
        }
    }

    public void SetDependencies(List<string> depenciesPaths)
    {
        var yaml = new YamlStream();
        using (var sr = new StreamReader(FullPath))
        {
            yaml.Load(sr);
        }
        var mapping =
            (YamlMappingNode)yaml.Documents[0].RootNode;
        var trigger = (YamlMappingNode)mapping.Children[new YamlScalarNode("trigger")];
        var paths = (YamlMappingNode)trigger.Children[new YamlScalarNode("paths")];
        var include = (YamlSequenceNode)paths.Children[new YamlScalarNode("include")];
        include.Children.Clear();
        foreach (var dependency in Global.BuildProps)
        {
            include.Children.Add(new YamlScalarNode(dependency));
        }
        foreach (var dependency in depenciesPaths)
        {
            include.Children.Add(new YamlScalarNode(dependency));
        }
        var buffer = new StringBuilder();
        var yamlStream = new YamlStream(yaml.Documents[0]);

        using (var writer = new StringWriter(buffer))
        {
            yamlStream.Save(writer, false);
        }
        using (TextWriter writer = File.CreateText(FullPath))
        {
            yamlStream.Save(writer, false);
        }
    }
}
