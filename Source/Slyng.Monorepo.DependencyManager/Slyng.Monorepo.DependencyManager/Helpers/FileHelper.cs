using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slyng.Monorepo.DependencyManager.Helpers
{
    public static class FileHelper
    {

        public static List<string> GetFilesByType(string rootFolder, string type = "*.*")
        {
            return Directory.EnumerateFiles(rootFolder, type, SearchOption.AllDirectories).ToList();
        }
        public static List<string> GetRelativeFilesByType(string rootFolder, string type = "*.*")
        {
            return Directory.EnumerateFiles(rootFolder, type, SearchOption.AllDirectories).Select(path => path.Replace(rootFolder, "")).ToList();
        }

        public static string ForwardSlashes(this string s)
        {
            return s.Replace("\\", "/");
        }


    }


}
