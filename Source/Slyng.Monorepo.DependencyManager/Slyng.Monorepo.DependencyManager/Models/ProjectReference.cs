using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Utilities;
using Slyng.Monorepo.DependencyManager.Helpers;
using System.Xml;
using System.Xml.Linq;

namespace Slyng.Monorepo.DependencyManager.Models
{
    public class ProjectReference
    {
        public ProjectReference(string path)
        {
            FullPath = path;
            ColorConsole.WriteEmbeddedColorLine($"   - Reference: [DarkYellow]{ProjectFileName}[/DarkYellow]");
        }


        public string FullPath { get; set; }

        public string FullDirectory
        {
            get
            {
                return Path.GetDirectoryName(FullPath) + "\\"; ;
            }
        }
        public string ProjectFileName
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

        /// <summary>
        /// Path relative to the solution
        /// </summary>
        public string RelativeSolutionDirectory
        {
            get
            {
                var directory = new DirectoryInfo(FullDirectory);
                while (directory != null && !directory.GetFiles("*.sln").Any())
                {
                    directory = directory.Parent;
                }
                return directory.FullName.Replace(Global.RootPath, "").TrimStart('\\');
            }
        }


    }
}
