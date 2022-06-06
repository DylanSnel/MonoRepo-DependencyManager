//using Microsoft.Build.Construction;
using Slyng.Monorepo.DependencyManager.Helpers;
using Slyng.Monorepo.DependencyManager.Integration;
using Slyng.Monorepo.DependencyManager.Models;
using System.Collections.Generic;
using System.Linq;

namespace Slyng.Monorepo.DependencyManager
{
    public class Program
    {
        static void Main(string[] args)
        {
            GitLogic git = new GitLogic();

            Global.RootPath = git.GetRootPath();
            Global.Solutions = FileHelper.GetFilesByType(Global.RootPath, "*.sln").Select(csproj => new SolutionFile(csproj)).ToList();
            foreach (var solution in Global.Solutions)
            {
                foreach (var project in solution.Projects)
                {
                    project.HandleReplaceMents();
                }
            }


        }
    }

    public static class Global
    {
        public static string RootPath { get; set; } = "";
        public static List<ProjectFile> Projects { get; set; } = new List<ProjectFile>();
        public static List<SolutionFile> Solutions { get; set; } = new List<SolutionFile>();
    }
}