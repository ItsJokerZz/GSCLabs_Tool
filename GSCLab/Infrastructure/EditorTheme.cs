using System.IO;
using System.Text.Json;

namespace GSCLab.Infrastructure
{
    public class EditorTheme
    {
        public static EditorTheme Current { get; set; } = new EditorTheme();

        public string CommentColor { get; set; } = "#6A9955";
        public string StringColor { get; set; } = "#CE9178";
        public string KeywordColor { get; set; } = "#C586C0";
        public string ScopeColor { get; set; } = "#569CD6";
        public string RtmColor { get; set; } = "#4EC9B0";
        public string FunctionColor { get; set; } = "#DCDCAA";
        public string NumberColor { get; set; } = "#B5CEA8";
        public string PointerColor { get; set; } = "#007ACC";
        public string BoolColor { get; set; } = "#9370DB";
        public string Ps4IP { get; set; }
        public bool UseNexus { get; set; }
        public bool UsePS4Debug { get; set; }

        public static void Load()
        {
            string path = "theme_settings.json";
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    Current = JsonSerializer.Deserialize<EditorTheme>(json) ?? new EditorTheme();
                }
                catch 
                {
                    
                }
            }
        }
    }
}
