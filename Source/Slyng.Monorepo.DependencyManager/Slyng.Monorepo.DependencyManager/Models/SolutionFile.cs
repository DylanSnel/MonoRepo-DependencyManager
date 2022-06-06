using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Utilities;
using Slyng.Monorepo.DependencyManager.Helpers;

namespace Slyng.Monorepo.DependencyManager.Models
{
    public class SolutionFile
    {
        public SolutionFile(string path)
        {
            FullPath = path;
            Console.WriteLine($"Solution: {SolutionFileName}");
            Projects = FileHelper.GetFilesByType(FullDirectory, "*.csproj").Select(csproj => new ProjectFile(csproj)).ToList();
        }


        public string FullPath { get; set; }
        public List<ProjectFile> Projects { get; private set; }

        public string FullDirectory
        {
            get
            {
                return Path.GetDirectoryName(FullPath) + "\\";
            }
        }
        public string SolutionFileName
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

        public string RelativeDirectory
        {
            get
            {
                return FullDirectory.Replace(Global.RootPath, "").TrimStart('\\');
            }
        }




    }
}
