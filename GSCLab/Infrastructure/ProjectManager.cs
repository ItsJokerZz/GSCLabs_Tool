using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GSCLab.Infrastructure
{
    public static class ProjectManager
    {
        public static string CurrentProjectPath { get; set; }
        public static string ActiveGame { get; set; }
        public static string ActiveMode { get; set; }
        public static string ActivePlatform { get; set; }

        public static event Action ProjectChanged;
        public static Views.ProjectConfigControl ConfigUI { get; set; }

        public static void LoadProject(string path)
        {
            CurrentProjectPath = path;
            ProjectChanged?.Invoke();
        }

        public static Dictionary<string, List<string>> GlobalFunctions = new Dictionary<string, List<string>>();

        public static void IndexProjectFunctions(string projectPath)
        {
            GlobalFunctions.Clear();
            if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath)) return;

            var files = Directory.GetFiles(projectPath, "*.gsc", SearchOption.AllDirectories);
            var funcRegex = new Regex(@"^([a-zA-Z_][a-zA-Z0-9_]*)\s*\(", RegexOptions.Multiline);

            foreach (var file in files)
            {
                string content = File.ReadAllText(file);
                string fileName = Path.GetFileName(file);

                var matches = funcRegex.Matches(content);
                var funcs = new List<string>();

                foreach (Match match in matches)
                {
                    funcs.Add(match.Groups[1].Value);
                }

                GlobalFunctions[fileName] = funcs;
            }
        }
        public static void NotifyProjectChanged()
        {
            ProjectChanged?.Invoke();
        }
    }
}
