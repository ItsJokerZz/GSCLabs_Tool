using DiscordRPC.Logging;
using GSCLab.Views;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GSCLab.Infrastructure
{
    public static class FunctionLibrary
    {
        public static Action<string> OnLogRequest;
        public static HashSet<string> MasterMethodList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> MasterTokenList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public static List<GscCompletionData> MasterCompletionList = new List<GscCompletionData>();

        public static void LoadMasterMetadata(Infrastructure.ServerProvider.GameMetadata data)
        {
            MasterMethodList.Clear();
            MasterCompletionList.Clear();
            MasterTokenList.Clear();

            if (data.methods != null)
            {
                foreach (var name in data.methods)
                {
                    MasterMethodList.Add(name.ToLower());
                    MasterCompletionList.Add(new GscCompletionData(name, $"[Method] - {data.game}", "🛠️"));
                }
            }

            if (data.functions != null)
            {
                foreach (var name in data.functions)
                {
                    MasterMethodList.Add(name.ToLower());
                    MasterCompletionList.Add(new GscCompletionData(name, $"[Function] - {data.game}", "ƒ"));
                }
            }

            if (data.tokens != null)
            {
                foreach (var name in data.tokens)
                {
                    MasterTokenList.Add(name.ToLower());
                    MasterMethodList.Add(name.ToLower());
                    MasterCompletionList.Add(new GscCompletionData(name, $"[Token] - {data.game}", "💎"));
                }
            }
        }

        public static bool IsKeyword(string name)
        {
            string[] gscKeywords =
            {
                "if", "else", "while", "for", "foreach", "switch", "case", "wait", "waittill", "notify", "endon", "return", "break", "continue", "prof_begin", "prof_end", "thread", "childthread", "waitframe", "writebyte", "writebytes", "writeint", "writeints", "writeshort", "writeshorts", "writestring", "writefloat", "writefloats", "readbyte", "readbytes", "readint", "readints", "readshort", "readshorts", "readfloat", "readfloats", "rpc", "xnotify", "logger", "readstring"
            };
            return gscKeywords.Contains(name.ToLower());
        }

        public static async Task LoadGameData(string gameName)
        {
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "games", gameName, "metadata.json");

                if (!File.Exists(filePath))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ToastNotification.Show("Error: Locating metadata.json");
                        _= Discord.SendDiscordError(Infrastructure.Session.Username + " Errror: location metadata.json");
                    });
                    return;
                }

                using (FileStream openStream = File.OpenRead(filePath))
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var data = await JsonSerializer.DeserializeAsync<Infrastructure.ServerProvider.GameMetadata>(openStream, options);

                    if (data != null)
                    {
                        LoadMasterMetadata(data);

                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ToastNotification.Show("Error Parsing Library File");
                });
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        public static bool ValidateCodeBeforeInject(string code, List<GscCompletionData> localFuncs, string fileName)
        {
            string[] lines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var localSet = new HashSet<string>(localFuncs.Select(f => f.Text), StringComparer.OrdinalIgnoreCase);
            bool inBlockComment = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string rawLine = lines[i];

                if (inBlockComment)
                {
                    if (rawLine.Contains("*/")) inBlockComment = false;
                    continue;
                }

                string trimmedLine = rawLine.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("//")) continue;

                if (trimmedLine.StartsWith("/*"))
                {
                    if (!trimmedLine.Contains("*/")) inBlockComment = true;
                    continue;
                }

                string lineWithoutStrings = Regex.Replace(trimmedLine, @"""([^""\\]|\\.)*""", "");

                var matches = Regex.Matches(lineWithoutStrings, @"\b([a-zA-Z_]\w*)\s*\(");

                foreach (Match match in matches)
                {
                    string funcName = match.Groups[1].Value;

                    if (IsKeyword(funcName)) { continue; }
                    if (funcName.StartsWith("_id_") || funcName.StartsWith("_meth_") || funcName.StartsWith("_func_")) { continue; }

                    string beforeMatch = lineWithoutStrings.Substring(0, match.Index).TrimEnd();
                    bool isFarcall = beforeMatch.EndsWith("::");

                    if (isFarcall) continue;

                    bool isTokenOnly = MasterTokenList.Contains(funcName.ToLower()) && !localSet.Contains(funcName);
                    bool isMaster = MasterMethodList.Contains(funcName.ToLower());
                    bool isLocal = localSet.Contains(funcName);

                    if (isTokenOnly && !isFarcall)
                    {
                        OnLogRequest?.Invoke($"^1[LOG] LINKER ERROR: '{funcName}' is not a callable function in {fileName} (Line {i + 1}) - did you mean a farcall like 'maps/mp/...:{funcName}'?");
                        return false;
                    }

                    if (!isMaster && !isLocal)
                    {
                        OnLogRequest?.Invoke($"^1[LOG] LINKER ERROR: Unknown function '{funcName}' in {fileName} (Line {i + 1})");
                        return false;
                    }
                }
            }
            return true;
        }
    }
}