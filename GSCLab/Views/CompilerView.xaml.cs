using DiscordRPC.Message;
using GSCLab.Infrastructure;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;

namespace GSCLab.Views
{
    public partial class CompilerView : UserControl
    {
        private CompletionWindow _completionWindow;
        private List<GscCompletionData> _gscFunctions = new List<GscCompletionData>();
        private Dictionary<SearchPanel, ICSharpCode.AvalonEdit.TextEditor> _editorMap = new Dictionary<SearchPanel, ICSharpCode.AvalonEdit.TextEditor>();
        private SearchPanel _searchPanel;
        public CompilerView()
        {

            InitializeComponent();

            OpenProjectView.CloseProject = this.CloseProject;
            DataContext = this;

            Infrastructure.FunctionLibrary.OnLogRequest = (msg) => {
                this.Log(msg);
            };

            DebugOutput.TextArea.TextView.LineTransformers.Add(new CoDColorTransformer());
            LoadGscHighlighting();
            LoadSavedTheme();
            ProjectManager.ProjectChanged += RefreshSolutionExplorer;

            var mainRenderer = new OccurrencesRenderer(GscEditor);
            GscEditor.TextArea.TextView.BackgroundRenderers.Add(mainRenderer);
            GscEditor.TextArea.Caret.PositionChanged += (s, e) =>
            {
                int offset = GscEditor.CaretOffset;
                string word = GetWordAtOffset(GscEditor.Document, offset);
                if (!string.IsNullOrWhiteSpace(word) && word.Length >= 3 && Regex.IsMatch(word, @"^[a-zA-Z0-9_]+$"))
                {
                    mainRenderer.WordToHighlight = word;
                }
                else
                {
                    mainRenderer.WordToHighlight = null;
                }

                GscEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
            };

            GscEditor.Options.ConvertTabsToSpaces = true;
            GscEditor.Options.IndentationSize = 4;

            GscEditor.TextArea.TextEntered += GscEditor_TextEntered;
            GscEditor.TextArea.TextEntering += GscEditor_TextEntering;
            GscEditor.TextArea.PreviewKeyDown += GscEditor_PreviewKeyDown;
            _searchPanel = SearchPanel.Install(GscEditor);
            _editorMap[_searchPanel] = GscEditor;

            SolutionTreeView.ContextMenuOpening += (s, e) =>
            {
                if (string.IsNullOrEmpty(ProjectManager.CurrentProjectPath))
                {
                    e.Handled = true;
                }
            };

            InitializeEnvironment();

            GscEditor.Text = "//Examples\r\n\r\n//Read Memory Example\r\n\r\nReadByte(0x542345); //Reads The Byte At 0x542345\r\nReadBytes(0x542345, 4);//Reads 4 Bytes At 0x542345\r\n\r\n//Write Memory Options Example\r\n\r\nWriteByte(0x542345, 0x45);//Writes 0x45 at 0x542345\r\nWriteBytes(0x542345, 0x45, 0x46, 0x47);//Writes 0x45, 0x46, 0x47 Starting At 0x542345\r\n\r\n//RPC Example\r\n\r\nRPC(0x542345, \"g_speed 999;\");//Cbuf_AddText\r\n\r\n//Logger Example\r\n\r\nLogger(self GetCurrentWeapon());//Prints To Log In The GSCLab (Compiler View)\r\n\r\n//xNotify Example\r\n\r\nxNotify(\"GSC Lab Notify\");//Send A Notify To Console PS4/Xbox 360 ETC\r\n\r\n//All Memory Options\r\n\r\n//Write Memory\r\nWriteByte\r\nWriteBytes\r\nWriteInt\r\nWriteInts\r\nWriteShort\r\nWriteShorts\r\nWriteString\r\nWriteFloat\r\nWriteFloats\r\n//Read Memory\r\nReadByte\r\nReadBytes\r\nReadInt\r\nReadInts\r\nReadShort\r\nReadShorts\r\nReadString\r\nReadFloat\r\nReadFloats\r\n//Other Stuff\r\nRPC\r\nxNotify\r\nLogger\r\n\r\n//Fuction Example\r\nFunction()\r\n{\r\n\tself iprintln(\"Function Test\");//This will print Function Test on your screen\r\n}\r\n\r\n//Color Codes\r\n/* \r\n*    ^1 = Red\r\n*    ^2 = Green\r\n*    ^3 = Yellow\r\n*    ^4 = Dark Blue\r\n*    ^5 = Cyan\r\n*    ^6 = Magenta/Pink\r\n*    ^7 = White\r\n*    ^8 = Different Per Game\r\n*    ^9 = Different Per Game\r\n*/\r\n\r\n//Hashed Function Calling Example\r\n\r\n//_id_7FEE = SetPoint\r\n//_id_A7A7 = maps\\mp\\gametypes\\_hud_util\r\n\r\nFunctionSetPoint()\r\n{\r\n\t//Normal If Not Hashed\r\n\t\r\n\ttextElem maps\\mp\\gametypes\\_hud_util::setpoint(align, relative, x, y);\r\n\t\r\n\t//If Function Is Hashed\r\n\t\r\n\ttextElem _id_A7A7::_id_7FEE(align, relative, x, y);\r\n}\r\n\r\n//Same Goes For _meth_ and _func_ \r\n\r\n//Overflow Example\r\n\r\ninit_overFlowFix()//Call This On init or onPlayerConnect or onPlayerSpawned\r\n{\r\n    level.overFlowFix_Started = true;\r\n    level.strings             = [];\r\n    \r\n    level.overflowElem = createServerFontString(\"default\",1.5);\r\n    level.overflowElem setText(\"overflow\");   \r\n    level.overflowElem.alpha = 0;\r\n    \r\n    level thread overflowfix_monitor();\r\n}\r\n\r\n\r\n_setText(string)//Instead Of SetText use this Function like textElem SetText(text) would be textElem _setText(text)\r\n{\r\n    self.string = string;\r\n    self setText(string);\r\n    self addString(string);\r\n    self thread fix_string();\r\n}\r\n\r\naddString(string)\r\n{\r\n    level.strings[level.strings.size] = string;\r\n    level notify(\"string_added\");\r\n}\r\n\r\nfix_string()\r\n{\r\n    self notify(\"new_string\");\r\n    self endon(\"new_string\");\r\n    while(isDefined(self))\r\n    {\r\n        level waittill(\"overflow_fixed\");\r\n        if(isDefined(self.string))\r\n        {\r\n            self _setText(self.string);\r\n        }\r\n    }\r\n}\r\n\r\noverflowfix_monitor()\r\n{  \r\n    level endon(\"game_ended\");\r\n    for(;;)\r\n    {\r\n\r\n        level waittill(\"string_added\");\r\n        if(level.strings.size >= 25)//This Number Differs Per Game 25/20 is AW\r\n        {\r\n            level.overflowElem clearalltextafterhudelem();\r\n            level.strings = [];\r\n            level notify(\"overflow_fixed\");\r\n        }\r\n        wait 0.01; \r\n    }\r\n}\r\n\r\ninArray(ar,string)\r\n{\r\n    for(i=0;i<ar.size;i++)\r\n    {\r\n        if(ar[i]==string)\r\n        {\r\n            return true;\r\n        }\r\n    }\r\n    return false;      \r\n}";
        }

        public ICommand CtrlSCommand => new RelayCommand(() =>
        {
            SaveProject(false);
        });

        public ICommand CtrlICommand => new RelayCommand(() =>
        {
            InjectButton_Click(null, null);
        });

        private async void InitializeEnvironment()
        {
            await Task.Run(async () =>
            {
                await GSCLab.Infrastructure.FunctionLibrary.LoadGameData("ghosts_ps4");
            });

            RefreshGlobalAutocomplete();
        }

        private void OpenProjectConfiguration()
        {
            var configPage = new GSCLab.Views.ProjectConfigControl();

            foreach (TabItem tab in EditorTabs.Items)
            {
                if (tab.Header.ToString() == "⚙️ config.gl")
                {
                    EditorTabs.SelectedItem = tab;
                    return;
                }
            }

            configPage.LoadConfiguration(ProjectManager.CurrentProjectPath);

            var configTab = new TabItem
            {
                Header = "⚙️ config.gl",
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")),
                Foreground = Brushes.White,

                Content = configPage
            };

            if (!EditorTabs.Items.Contains(configTab))
            {
                EditorTabs.Items.Add(configTab);
                EditorTabs.SelectedItem = configTab;
            }
        }

        private void LoadSavedTheme()
        {
            string configPath = "theme_settings.json";

            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    var savedTheme = JsonSerializer.Deserialize<EditorTheme>(json);

                    if (savedTheme != null)
                    {
                        UpdateHighlightColor("Comment", (Color)ColorConverter.ConvertFromString(savedTheme.CommentColor));
                        UpdateHighlightColor("String", (Color)ColorConverter.ConvertFromString(savedTheme.StringColor));
                        UpdateHighlightColor("Keywords", (Color)ColorConverter.ConvertFromString(savedTheme.KeywordColor));
                        UpdateHighlightColor("Scope", (Color)ColorConverter.ConvertFromString(savedTheme.ScopeColor));
                        UpdateHighlightColor("RTMCalls", (Color)ColorConverter.ConvertFromString(savedTheme.RtmColor));
                        UpdateHighlightColor("Functions", (Color)ColorConverter.ConvertFromString(savedTheme.FunctionColor));
                        UpdateHighlightColor("Numbers", (Color)ColorConverter.ConvertFromString(savedTheme.NumberColor));
                        UpdateHighlightColor("MethodPointer", (Color)ColorConverter.ConvertFromString(savedTheme.PointerColor));
                        UpdateHighlightColor("Bools", (Color)ColorConverter.ConvertFromString(savedTheme.BoolColor));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Theme failed to load: " + ex.Message);
                }
            }
        }

        private void GscEditor_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ICSharpCode.AvalonEdit.TextEditor editor = null;
            ICSharpCode.AvalonEdit.Editing.TextArea textArea = null;

            if (sender is ICSharpCode.AvalonEdit.TextEditor ed)
            {
                editor = ed;
                textArea = ed.TextArea;
            }
            else if (sender is ICSharpCode.AvalonEdit.Editing.TextArea ta)
            {
                textArea = ta;
                editor = FindParent<ICSharpCode.AvalonEdit.TextEditor>(ta);
            }

            if (textArea == null) return;

            if (Keyboard.Modifiers == ModifierKeys.Control && (e.Key == Key.OemQuestion || e.Key == Key.Divide))
            {
                if (editor != null)
                {
                    ToggleComments(editor);
                    e.Handled = true;
                    return;
                }
            }

            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.I)
            {
                CtrlICommand?.Execute(null);
                e.Handled = true;
                return;
            }

            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.D)
            {
                var doc = textArea.Document;
                if (doc != null)
                {
                    string textToDupe = "";
                    int insertPosition = 0;

                    if (textArea.Selection.Length > 0)
                    {
                        int selStart = textArea.Document.GetOffset(textArea.Selection.StartPosition.Location);
                        int selEnd = textArea.Document.GetOffset(textArea.Selection.EndPosition.Location);

                        var startLine = doc.GetLineByOffset(Math.Min(selStart, selEnd));
                        var endLine = doc.GetLineByOffset(Math.Max(selStart, selEnd) - 1);

                        int startOffset = startLine.Offset;
                        int endOffset = endLine.EndOffset;

                        int length = endOffset - startOffset;

                        if (length > 0)
                        {
                            textToDupe = Environment.NewLine + doc.GetText(startOffset, length);
                            insertPosition = endOffset;
                        }
                    }
                    else
                    {
                        var currentLine = doc.GetLineByOffset(textArea.Caret.Offset);
                        insertPosition = currentLine.EndOffset;

                        textToDupe = Environment.NewLine + doc.GetText(currentLine.Offset, currentLine.Length);
                    }

                    using (doc.RunUpdate())
                    {
                        doc.Insert(insertPosition, textToDupe);
                        textArea.Caret.Offset = insertPosition + textToDupe.Length;
                    }

                    e.Handled = true;
                    return;
                }
            }

            if (_completionWindow != null)
            {
                if (e.Key == System.Windows.Input.Key.Back)
                {
                    if (textArea.Caret.Offset - 1 <= _completionWindow.StartOffset)
                    {
                        _completionWindow.Close();
                    }
                }
                else if (e.Key == System.Windows.Input.Key.Escape)
                {
                    _completionWindow.Close();
                }
            }

            if (e.Key == Key.Enter)
            {
                var doc = textArea.Document;
                int offset = textArea.Caret.Offset;

                if (offset > 0 && offset < doc.TextLength)
                {
                    char before = doc.GetCharAt(offset - 1);
                    char after = doc.GetCharAt(offset);

                    if (before == '{' && after == '}')
                    {
                        e.Handled = true;

                        ICSharpCode.AvalonEdit.Document.DocumentLine line = doc.GetLineByOffset(offset);
                        string lineText = doc.GetText(line);

                        string baseIndent = new string(lineText.TakeWhile(char.IsWhiteSpace).ToArray());
                        string indentUnit = baseIndent.Contains('\t') ? "\t" : (IndentUnit ?? "    ");
                        string innerIndent = baseIndent + indentUnit;
                        string insertText = Environment.NewLine + innerIndent + Environment.NewLine + baseIndent;

                        using (doc.RunUpdate())
                        {
                            doc.Insert(offset, insertText);
                            textArea.Caret.Offset = offset + Environment.NewLine.Length + innerIndent.Length;
                        }
                    }
                }
            }
        }

        private void RefreshGlobalAutocomplete()
        {
            _gscFunctions.Clear();

            if (FunctionLibrary.MasterCompletionList.Count > 0)
                _gscFunctions.AddRange(FunctionLibrary.MasterCompletionList);

            string projectPath = ProjectManager.CurrentProjectPath;
            var funcRegex = new Regex(@"^\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*\([\s\S]*?\)\s*\{", RegexOptions.Multiline);

            if (!string.IsNullOrEmpty(projectPath) && Directory.Exists(projectPath))
            {
                var files = Directory.GetFiles(projectPath, "*.gsc", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    try
                    {
                        string content = File.ReadAllText(file);
                        ParseContentForFunctions(content, Path.GetFileName(file));
                    }
                    catch { }
                }
            }

            foreach (TabItem tab in EditorTabs.Items)
            {
                if (tab.Content is ICSharpCode.AvalonEdit.TextEditor editor)
                {
                    ParseContentForFunctions(editor.Text, tab.Header.ToString());
                }
            }
        }

        private List<string> GetRealFunctionDefinitions(string content)
        {
            var names = new List<string>();

            string cleanCode = Regex.Replace(content, @"//.*|/\*[\s\S]*?\*/", "");
            string[] lines = cleanCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                bool isDefinition =
                    Regex.IsMatch(line, @"^\s*[a-zA-Z_]\w*\s*\([^)]*\)\s*\{") ||
                    (Regex.IsMatch(line, @"^\s*[a-zA-Z_]\w*\s*\([^)]*\)\s*$") &&
                     i + 1 < lines.Length &&
                     lines[i + 1].Trim() == "{");

                if (!isDefinition) continue;

                var match = Regex.Match(line, @"\b([a-zA-Z_]\w*)\s*\(");
                if (!match.Success) continue;

                string name = match.Groups[1].Value;
                if (Infrastructure.FunctionLibrary.IsKeyword(name)) continue;

                names.Add(name);
            }

            return names;
        }

        private void ParseContentForFunctions(string content, string sourceName)
        {
            if (_gscFunctions == null) return;

            string cleanCode = Regex.Replace(content, @"//.*|/\*[\s\S]*?\*/", "");
            string[] lines = cleanCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                bool isDefinition = false;

                if (Regex.IsMatch(line, @"^\s*[a-zA-Z_]\w*\s*\([^)]*\)\s*\{"))
                {
                    isDefinition = true;
                }
                else if (
                    Regex.IsMatch(line, @"^\s*[a-zA-Z_]\w*\s*\([^)]*\)\s*$") &&
                    i + 1 < lines.Length &&
                    lines[i + 1].Trim() == "{")
                {
                    isDefinition = true;
                }

                if (!isDefinition)
                    continue;

                var match = Regex.Match(line, @"\b([a-zA-Z_]\w*)\s*\(");
                if (!match.Success) continue;

                string name = match.Groups[1].Value;

                if (Infrastructure.FunctionLibrary.IsKeyword(name)) continue;

                lock (_gscFunctions)
                {
                    if (!_gscFunctions.Any(f => f != null && f.Text != null && f.Text.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    {
                        _gscFunctions.Add(new GscCompletionData(name, $"Local Function in {sourceName}"));
                    }
                }
            }
        }

        private void GscEditor_TextEntered(object sender, TextCompositionEventArgs e)
        {
            var textArea = sender as ICSharpCode.AvalonEdit.Editing.TextArea;
            if (textArea == null) return;

            if (e.Text == "{" || e.Text == "(" || e.Text == "[")
            {
                int offset = textArea.Caret.Offset;
                string closing = e.Text == "{" ? "}" : (e.Text == "(" ? ")" : "]");
                char closingChar = closing[0];

                if (offset < textArea.Document.TextLength && textArea.Document.GetCharAt(offset) == closingChar)
                {
                }
                else
                {
                    textArea.Document.Insert(offset, closing);
                    textArea.Caret.Offset -= 1;
                }
            }

            string currentWord = GetWordBeforeCaret(textArea, textArea.Caret.Offset);

            if (_completionWindow != null)
            {
                var filteredMatches = _gscFunctions.Where(f => f.Text.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase)).ToList();
                _completionWindow.CompletionList.CompletionData.Clear();
                foreach (var m in filteredMatches) _completionWindow.CompletionList.CompletionData.Add(m);

                if (filteredMatches.Count == 0) _completionWindow.Close();
                return;
            }

            if (e.Text.Length > 0 && (char.IsLetter(e.Text[0]) || e.Text[0] == '_' || e.Text[0] == '/'))
            {
                var matches = _gscFunctions.Where(f => f.Text.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase)).ToList();
                if (matches.Count > 0)
                {
                    _completionWindow = new CompletionWindow(textArea);
                    _completionWindow.Closed += (s, args) => _completionWindow = null;
                    _completionWindow.StartOffset = textArea.Caret.Offset - currentWord.Length;

                    ApplyCompletionWindowStyles(_completionWindow);

                    foreach (var m in matches) _completionWindow.CompletionList.CompletionData.Add(m);
                    _completionWindow.Show();
                }
            }
        }


        private void ApplyCompletionWindowStyles(CompletionWindow window)
        {
            window.Width = 500;
            window.MaxHeight = 295;
            window.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A"));
            window.Foreground = Brushes.White;
            window.BorderThickness = new Thickness(1);
            window.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
            window.WindowStyle = WindowStyle.None;
            window.AllowsTransparency = true;

            window.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 15,
                Direction = 270,
                ShadowDepth = 3,
                Opacity = 0.5
            };

            string xamlTemplate = @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'> <Grid Margin='0,2'> <Grid.ColumnDefinitions> <ColumnDefinition Width='25'/> <ColumnDefinition Width='*'/> </Grid.ColumnDefinitions><TextBlock Grid.Column='0' Text='{Binding Icon}' Foreground='#A0A0A0' HorizontalAlignment='Center' VerticalAlignment='Center' FontSize='12'/> <TextBlock Grid.Column='1' Text='{Binding Text}' Foreground='White' FontFamily='Consolas' FontSize='13' VerticalAlignment='Center'/> </Grid> </DataTemplate>";
            string resourcesXaml = @"<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'> <Style TargetType='{x:Type ScrollBar}'> <Setter Property='Width' Value='6'/> <Setter Property='Template'> <Setter.Value> <ControlTemplate TargetType='{x:Type ScrollBar}'> <Grid Background='Transparent'> <Track x:Name='PART_Track' IsDirectionReversed='true'>  <Track.Thumb> <Thumb> <Thumb.Template> <ControlTemplate TargetType='{x:Type Thumb}'> <Border Background='#007ACC' CornerRadius='3' /> </ControlTemplate> </Thumb.Template> </Thumb> </Track.Thumb> </Track> </Grid> </ControlTemplate> </Setter.Value> </Setter> </Style> <Style TargetType='{x:Type ListBoxItem}'> <Setter Property='Background' Value='Transparent'/> <Setter Property='Template'> <Setter.Value> <ControlTemplate TargetType='{x:Type ListBoxItem}'> <Border x:Name='Bd' Background='{TemplateBinding Background}' Padding='2'> <ContentPresenter /> </Border> <ControlTemplate.Triggers> <Trigger Property='IsSelected' Value='true'> <Setter TargetName='Bd' Property='Background' Value='#252526'/> <Setter TargetName='Bd' Property='BorderBrush' Value='#007ACC'/> <Setter TargetName='Bd' Property='BorderThickness' Value='2,0,0,0'/> </Trigger> </ControlTemplate.Triggers> </ControlTemplate> </Setter.Value> </Setter> </Style> </ResourceDictionary>";

            try
            {
                var context = new System.Windows.Markup.ParserContext();
                context.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
                window.CompletionList.ListBox.ItemTemplate = (DataTemplate)System.Windows.Markup.XamlReader.Parse(xamlTemplate, context);
                window.CompletionList.Resources.MergedDictionaries.Add((ResourceDictionary)System.Windows.Markup.XamlReader.Parse(resourcesXaml, context));
                window.CompletionList.ListBox.Background = Brushes.Transparent;
                window.CompletionList.ListBox.BorderThickness = new Thickness(0);
            }
            catch { }

            Style toolTipStyle = new Style(typeof(ToolTip));
            toolTipStyle.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#252526"))));
            toolTipStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            toolTipStyle.Setters.Add(new Setter(Control.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#444444"))));
            toolTipStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
            window.Resources.Add(typeof(ToolTip), toolTipStyle);
        }

        private void GscEditor_TextEntering(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            var textArea = sender as ICSharpCode.AvalonEdit.Editing.TextArea;

            if (textArea != null && e.Text.Length > 0 &&
                (e.Text == "}" || e.Text == ")" || e.Text == "]"))
            {
                char closingChar = e.Text[0];
                int offset = textArea.Caret.Offset;

                if (offset < textArea.Document.TextLength && textArea.Document.GetCharAt(offset) == closingChar)
                {
                    textArea.Caret.Offset += 1;
                    e.Handled = true;
                    return;
                }
            }

            if (e.Text.Length > 0 && _completionWindow != null)
            {
                char c = e.Text[0];
                if (c == ' ')
                {
                    _completionWindow.Close();
                    return;
                }

                if (!char.IsLetterOrDigit(c) && c != '_' && c != '/' && c != '\\' && c != ':')
                {
                    _completionWindow.Close();
                }
            }
        }

        private string GetWordBeforeCaret(ICSharpCode.AvalonEdit.Editing.TextArea textArea, int offset)
        {
            int startOffset = offset;
            var document = textArea.Document;

            while (startOffset > 0)
            {
                char c = document.GetCharAt(startOffset - 1);
                if (char.IsLetterOrDigit(c) || c == '_' || c == '/' || c == '\\' || c == ':')
                    startOffset--;
                else
                    break;
            }

            return document.GetText(startOffset, offset - startOffset);
        }

        private void OpenProjectButton_Click(object sender, RoutedEventArgs e)
        {
            OpenProjectView open = new OpenProjectView();
            open.ShowDialog();
        }

        private async void RefreshSolutionExplorer()
        {
            List<string> expandedPaths = GetExpandedPaths(SolutionTreeView.Items);

            string path = ProjectManager.CurrentProjectPath;
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;

            bool isUpdate = SolutionTreeView.Items.Count > 0;

            if (!isUpdate)
            {
                CloseProjectV2();

                if (tabExamples.Parent is TabControl tabControl)
                {
                    tabControl.Items.Remove(tabExamples);
                }
            }

            SolutionTreeView.Items.Clear();

            var root = new TreeViewItem
            {
                Header = $"📁 {Path.GetFileName(path).Replace("_", "__")}",
                Tag = path,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                FontWeight = System.Windows.FontWeights.Bold,
                IsExpanded = true
            };

            LoadDirectory(path, root, expandedPaths);
            SolutionTreeView.Items.Add(root);

            if (isUpdate)
            {
                RefreshGlobalAutocomplete();
                return;
            }

            OpenProjectConfiguration();

            if (System.IO.File.Exists(System.IO.Path.Combine(ProjectManager.CurrentProjectPath, "main.gsc")))
            {
                OpenGscEditorTab(System.IO.Path.Combine(ProjectManager.CurrentProjectPath, "main.gsc"));
            }

            RefreshGlobalAutocomplete();
        }

        public void CloseProject()
        {
            ProjectManager.CurrentProjectPath = "";

            SolutionTreeView.Items.Clear();

            EditorTabs.Items.Clear();
            EditorTabsRight.Items.Clear();

            UpdateSplitViewVisibility();
        }

        public void CloseProjectV2()
        {
            SolutionTreeView.Items.Clear();

            EditorTabs.Items.Clear();
            EditorTabsRight.Items.Clear();

            UpdateSplitViewVisibility();
        }

        private void LoadDirectory(string sourceDir, TreeViewItem parentItem, List<string> expandedPaths)
        {
            foreach (string directory in Directory.GetDirectories(sourceDir))
            {
                var directoryItem = new TreeViewItem
                {
                    Header = $"📁 {Path.GetFileName(directory)}",
                    Tag = directory,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                    FontWeight = System.Windows.FontWeights.Bold,
                    IsExpanded = expandedPaths.Contains(directory)
                };

                LoadDirectory(directory, directoryItem, expandedPaths);
                parentItem.Items.Add(directoryItem);
            }

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                if (fileName.EndsWith(".gsc") || fileName == "config.gl")
                {
                    var fileItem = new TreeViewItem
                    {
                        Header = $"{(fileName == "config.gl" ? "⚙️" : "📄")} {fileName.Replace("_", "__")}",
                        Foreground = Brushes.White,
                        Tag = file
                    };
                    parentItem.Items.Add(fileItem);
                }
            }
        }

        private void NewProjectButton_Click(object sender, RoutedEventArgs e)
        {
            NewProjectView newProject = new NewProjectView();
            newProject.ShowDialog();
        }

        public void LoadGscHighlighting()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GSC.xshd");

            if (File.Exists(path))
            {
                using (XmlTextReader reader = new XmlTextReader(path))
                {
                    GscEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                }
            }
        }

        public void UpdateHighlightColor(string colorName, Color newColor)
        {
            var color = GscEditor.SyntaxHighlighting.GetNamedColor(colorName);

            if (color != null)
            {
                color.Foreground = new SimpleHighlightingBrush(newColor);

                GscEditor.TextArea.TextView.Redraw();

                foreach (TabItem tab in EditorTabs.Items)
                {
                    if (tab.Content is ICSharpCode.AvalonEdit.TextEditor dynamicEditor)
                    {
                        dynamicEditor.TextArea.TextView.Redraw();
                    }
                }
            }
        }

        private const string IndentUnit = "    ";

        private void EditorSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            EditorSettings editor = new EditorSettings(this);
            editor.ShowDialog();
        }

        public string GetCurrentHighlightColorName(string colorName)
        {
            var highlightingColor = GscEditor.SyntaxHighlighting.GetNamedColor(colorName);

            if (highlightingColor?.Foreground != null)
            {
                var brush = highlightingColor.Foreground.GetBrush(null) as SolidColorBrush;
                if (brush != null)
                {
                    return brush.Color.ToString();
                }
            }
            return null;
        }

        private void SaveProject(bool showSave)
        {
            var allTabs = EditorTabs.Items.OfType<TabItem>().Concat(EditorTabsRight.Items.OfType<TabItem>()).ToList();

            if (allTabs.Count == 0) return;

            foreach (TabItem tab in allTabs)
            {
                if (tab.Content is GSCLab.Views.ProjectConfigControl configControl)
                {
                    configControl.SaveConfiguration(ProjectManager.CurrentProjectPath);
                }
                else if (tab.Content is ICSharpCode.AvalonEdit.TextEditor editor)
                {
                    string filePath = tab.Tag?.ToString()!;

                    if (tab.Header.ToString().EndsWith("*"))
                    {
                        tab.Header = tab.Header.ToString().Replace("*", "");
                    }

                    if (!string.IsNullOrEmpty(filePath))
                    {
                        editor.Save(filePath);
                    }
                }
            }

            RefreshGlobalAutocomplete();

            if (showSave)
            {
                ToastNotification.Show("Project Saved");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveProject(true);
        }

        private void SolutionTreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = VisualUpwardSearch<TreeViewItem>(e.OriginalSource as DependencyObject);
            if (item == null) return;

            string filePath = item.Tag?.ToString();

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                string fileName = Path.GetFileName(filePath);

                e.Handled = true;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (fileName == "config.gl")
                    {
                        OpenProjectConfiguration();
                    }
                    else if (fileName.EndsWith(".gsc"))
                    {
                        OpenGscEditorTab(filePath);
                    }
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
        }
        private static T? VisualUpwardSearch<T>(DependencyObject? source) where T : DependencyObject
        {
            while (source != null)
            {
                if (source is T target)
                    return target;

                if (source is Visual || source is System.Windows.Media.Media3D.Visual3D)
                {
                    source = System.Windows.Media.VisualTreeHelper.GetParent(source);
                }
                else
                {
                    source = System.Windows.LogicalTreeHelper.GetParent(source);
                }
            }
            return null;
        }

        private ICSharpCode.AvalonEdit.TextEditor OpenGscEditorTab(string filePath)
        {
            string fileName = System.IO.Path.GetFileName(filePath);

            var openTabs = EditorTabs.Items.OfType<TabItem>().Concat(EditorTabsRight.Items.OfType<TabItem>());
            foreach (TabItem tab in openTabs)
            {
                if (tab.Tag?.ToString() == filePath && tab.Content is ICSharpCode.AvalonEdit.TextEditor existingEditor)
                {
                    if (EditorTabs.Items.Contains(tab))
                    {
                        EditorTabs.SelectedItem = tab;
                    }
                    else if (EditorTabsRight.Items.Contains(tab))
                    {
                        EditorTabsRight.SelectedItem = tab;
                    }
                    existingEditor.Focus();
                    System.Windows.Input.Keyboard.Focus(existingEditor);

                    return existingEditor;
                }
            }

            var newEditor = new ICSharpCode.AvalonEdit.TextEditor
            {
                ShowLineNumbers = true,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F0F0F")),
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                SyntaxHighlighting = GscEditor.SyntaxHighlighting,
                Tag = filePath
            };

            newEditor.Options.ConvertTabsToSpaces = true;
            newEditor.Options.IndentationSize = 4;

            newEditor.TextArea.TextEntered += GscEditor_TextEntered;
            newEditor.TextArea.TextEntering += GscEditor_TextEntering;
            newEditor.TextArea.PreviewKeyDown += GscEditor_PreviewKeyDown;

            var conditionalColorizer = new GscConditionalColorizer();
            newEditor.TextArea.TextView.LineTransformers.Add(conditionalColorizer);

            Infrastructure.ProjectManager.ConfigUI.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "SelectedGameCode" || e.PropertyName == "SelectedPlatform")
                    RefreshGlobalAutocomplete();
            };

            var foldingManager = FoldingManager.Install(newEditor.TextArea);
            var foldingStrategy = new GscFoldingStrategy();

            var margin = newEditor.TextArea.LeftMargins.OfType<FoldingMargin>().FirstOrDefault();
            if (margin != null)
            {
                margin.FoldingMarkerBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));
                margin.SelectedFoldingMarkerBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204));
                margin.FoldingMarkerBackgroundBrush = Brushes.Transparent;
            }

            var newSearchPanel = ICSharpCode.AvalonEdit.Search.SearchPanel.Install(newEditor);
            _editorMap[newSearchPanel] = newEditor;

            newEditor.Load(filePath);

            foldingStrategy.UpdateFoldings(foldingManager, newEditor.Document);
            conditionalColorizer.UpdateBlocks(newEditor.Document);

            var newTab = new TabItem
            {
                Header = $"📄 {fileName}",
                Tag = filePath,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")),
                Foreground = Brushes.White,
                Content = newEditor
            };

            EditorTabs.Items.Add(newTab);
            EditorTabs.SelectedItem = newTab;

            newEditor.Focus();
            System.Windows.Input.Keyboard.Focus(newEditor);

            SetupEditorContextMenu(newEditor);

            var occurrenceRenderer = new OccurrencesRenderer(newEditor);
            newEditor.TextArea.TextView.BackgroundRenderers.Add(occurrenceRenderer);

            var bracketRenderer = new BracketGuideRenderer();
            newEditor.TextArea.TextView.BackgroundRenderers.Add(bracketRenderer);

            newEditor.TextChanged += (s, args) =>
            {
                foldingStrategy.UpdateFoldings(foldingManager, newEditor.Document);
                conditionalColorizer.UpdateBlocks(newEditor.Document);

                if (!newTab.Header.ToString().EndsWith("*"))
                {
                    newTab.Header += "*";
                }

                occurrenceRenderer.WordToHighlight = null;
                newEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
            };

            newEditor.TextArea.Caret.PositionChanged += (s, e) =>
            {
                int offset = newEditor.CaretOffset;
                var doc = newEditor.Document;

                int highlightStart = -1;
                int highlightEnd = -1;

                if (offset >= 0 && offset <= doc.TextLength)
                {
                    for (int i = 0; i <= 1; i++)
                    {
                        int checkPos = offset - i;
                        if (checkPos < 0 || checkPos >= doc.TextLength) continue;

                        char c = doc.GetCharAt(checkPos);
                        if ("{([".Contains(c))
                        {
                            char closer = c == '{' ? '}' : (c == '(' ? ')' : ']');
                            int match = FindMatchingBracket(doc, checkPos, c, closer, 1);
                            if (match != -1) { highlightStart = checkPos; highlightEnd = match; break; }
                        }
                        else if ("})]".Contains(c))
                        {
                            char opener = c == '}' ? '{' : (c == ')' ? '(' : '[');
                            int match = FindMatchingBracket(doc, checkPos, c, opener, -1);
                            if (match != -1) { highlightStart = match; highlightEnd = checkPos; break; }
                        }
                    }
                }

                bracketRenderer.StartOffset = highlightStart;
                bracketRenderer.EndOffset = highlightEnd;
                newEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);

                if (newEditor.TextArea.Selection.Length > 0) return;

                string word = GetWordAtOffset(doc, offset);
                if (!string.IsNullOrWhiteSpace(word) && word.Length >= 3 && Regex.IsMatch(word, @"^[a-zA-Z0-9_]+$"))
                {
                    occurrenceRenderer.WordToHighlight = word;
                }
                else
                {
                    occurrenceRenderer.WordToHighlight = null;
                }
                newEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
            };

            return newEditor;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseProject();
        }

        private async void InjectButton_Click(object sender, RoutedEventArgs e)
        {
            Log("^5[LOG] INFO: Starting Pre-Injection Check...");
            var config = Infrastructure.ProjectManager.ConfigUI;
            if (config == null) return;

            string vpsGameKey = "";

            SaveProject(false);

            try
            {
                string game = config.SelectedGameCode == "unknown" ? ProjectManager.ActiveGame : config.SelectedGameCode;
                string platform = config.SelectedPlatform == "unknown" ? ProjectManager.ActivePlatform : config.SelectedPlatform;
                string mode = config.SelectedModeCode == "unknown" ? ProjectManager.ActiveMode : config.SelectedModeCode;

                string gameName = game.ToUpper();

                if (game == "aw") game = "s1";
                else if (game == "mwr") game = "h1";
                else if (game == "ghosts") game = "iw6";
                else if (game == "mw2cr") game = "h2";
                else if (game == "ww2") game = "s2";
                else if (game == "mw3") game = "iw5";
                else if (game == "iw") game = "iw7";
                else if (game == "vanguard") game = "s4";
                else if (game == "mw22") game = "iw9";
                else if (game == "mw2019") game = "iw8";

                string platLower = platform.ToLower();

                if (game == "bo3" && platLower == "ps4")
                    vpsGameKey = "bo3_ps4";
                else
                    vpsGameKey = $"{game}{platLower}";

                if (vpsGameKey == "s1ccapi" || vpsGameKey == "s1hen") vpsGameKey = "s1ps";
                else if (vpsGameKey == "iw6ccapi" || vpsGameKey == "iw6hen") vpsGameKey = "iw6ps";
                else if (vpsGameKey == "iw5ccapi" || vpsGameKey == "iw5hen") vpsGameKey = "iw5ps";
                else if (vpsGameKey == "s4ps4") vpsGameKey = "s4";
                else if (vpsGameKey == "iw9ps4") vpsGameKey = "iw9";
                else if (vpsGameKey == "iw8ps4") vpsGameKey = "iw8";
                else if (vpsGameKey == "mw1ccapi") vpsGameKey = "mw1";

                string fileNameForVPS = (vpsGameKey == "bo3_ps4") ? "script.gsc" : "main.gsc";

                string projectPath = Infrastructure.ProjectManager.CurrentProjectPath;
                if (string.IsNullOrEmpty(projectPath)) return;

                string[] gscFiles = Directory.GetFiles(projectPath, "*.gsc", SearchOption.AllDirectories);
                var allProjectFunctions = new List<GscCompletionData>();
                foreach (var file in gscFiles)
                {
                    string content = File.ReadAllText(file);

                    var defs = Regex.Matches(content, @"^\s*([a-zA-Z_]\w*)\s*\(", RegexOptions.Multiline);

                    foreach (var name in GetRealFunctionDefinitions(content))
                    {
                        allProjectFunctions.Add(new GscCompletionData(name, "Local", ""));
                    }
                }

                StringBuilder masterBuilder = new StringBuilder();

                foreach (string file in gscFiles)
                {
                    string content = File.ReadAllText(file);
                    string fileName = Path.GetFileName(file);

                    content = SanitizeGscCode(content, game, gameName, mode);

                    content = PreProcessGscMemoryCalls(content);

                    var syntaxCheck = Infrastructure.SyntaxChecker.Validate(content, fileName);
                    if (!syntaxCheck.IsValid)
                    {
                        Log(syntaxCheck.ErrorMessage);
                        return;
                    }

                    bool isLinkerValid = Infrastructure.FunctionLibrary.ValidateCodeBeforeInject(content, allProjectFunctions, fileName);

                    if (!isLinkerValid)
                    {
                        Log("^1[LOG] FAILED: Linker check failed. Unknown functions found.");
                        return;
                    }

                    masterBuilder.AppendLine($"// File: {fileName}");
                    masterBuilder.AppendLine(content);
                    masterBuilder.AppendLine();
                }

                Log("^5[LOG] INFO: All checks passed.");



                byte[] compiledBytes = await RequestCompile(vpsGameKey, fileNameForVPS, masterBuilder.ToString());

                if (compiledBytes != null && compiledBytes.Length > 0)
                {
                    string responseTest = Encoding.UTF8.GetString(compiledBytes);
                    if (responseTest.StartsWith("ERROR"))
                    {
                        Log($"^1[LOG] ERROR: {responseTest}");
                        return;
                    }

                    var injection = await Connections.InjectionHandler.ProcessInjection(game, platform, mode, compiledBytes);

                    if (injection.Success)
                    {
                        Log($"^2[LOG] SUCCESS: Project Injected into {gameName.ToUpper()}");
                    }
                    else
                    {
                        Log($"^1[LOG] ERROR: Injection Failed - {injection.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"^1[LOG] SYSTEM ERROR: {ex.Message}");
            }
        }

        private double _lastOutputHeight = 180;

        private void ToggleOutput_Click(object sender, RoutedEventArgs e)
        {
            if (OutputRow.Height.Value > 0)
            {
                _lastOutputHeight = OutputRow.Height.Value;
                OutputRow.Height = new GridLength(0);
                OutputBorder.Visibility = Visibility.Collapsed;
            }
            else
            {
                OutputRow.Height = new GridLength(_lastOutputHeight);
                OutputBorder.Visibility = Visibility.Visible;
            }
        }

        private void AddGscFile_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ProjectManager.CurrentProjectPath)) return;

            string targetPath = ProjectManager.CurrentProjectPath;
            if (SolutionTreeView.SelectedItem is TreeViewItem selected && selected.Tag != null)
            {
                string path = selected.Tag.ToString()!;
                targetPath = Directory.Exists(path) ? path : Path.GetDirectoryName(path)!;
            }

            var dialog = new RenameDialog("new_script.gsc", "CREATE NEW GSC", "CREATE");
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true)
            {
                string fileName = dialog.NewName;

                if (!fileName.ToLower().EndsWith(".gsc"))
                {
                    fileName += ".gsc";
                }

                string fullPath = Path.Combine(targetPath, fileName);

                if (File.Exists(fullPath))
                {
                    MessageBox.Show("A file with that name already exists!", "GSCLab", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                File.WriteAllText(fullPath, "//GSCLab Generated Script");

                if (SolutionTreeView.SelectedItem is TreeViewItem selectedNode)
                    selectedNode.IsExpanded = true;

                RefreshSolutionExplorer();

                OpenGscEditorTab(fullPath);
            }
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ProjectManager.CurrentProjectPath)) return;

            string targetPath = ProjectManager.CurrentProjectPath;

            if (SolutionTreeView.SelectedItem is TreeViewItem selected && selected.Tag != null)
            {
                string path = selected.Tag.ToString()!;

                targetPath = Directory.Exists(path) ? path : Path.GetDirectoryName(path)!;

                selected.IsExpanded = true;
            }

            var dialog = new RenameDialog("New Folder", "CREATE NEW FOLDER", "CREATE");
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true)
            {
                string newFolderPath = Path.Combine(targetPath, dialog.NewName);

                if (Directory.Exists(newFolderPath))
                {
                    MessageBox.Show("A folder with that name already exists!", "GSCLab", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    Directory.CreateDirectory(newFolderPath);
                    RefreshSolutionExplorer();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not create folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (SolutionTreeView.SelectedItem is TreeViewItem selected && selected.Tag != null)
            {
                string path = selected.Tag.ToString()!;
                string name = Path.GetFileName(path);

                var dialog = new ConfirmDeleteDialog(name);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        if (File.Exists(path))
                        {
                            CloseTabByFilePath(path);
                            File.Delete(path);
                        }
                        else if (Directory.Exists(path))
                        {
                            Directory.Delete(path, true);
                        }

                        RefreshSolutionExplorer();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting: {ex.Message}", "GSCLab", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void RenameItem_Click(object sender, RoutedEventArgs e)
        {
            if (SolutionTreeView.SelectedItem is TreeViewItem selected && selected.Tag != null)
            {
                string oldPath = selected.Tag.ToString()!;
                string oldName = Path.GetFileName(oldPath)!;

                var dialog = new RenameDialog(oldName);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    string newName = dialog.NewName;
                    string parentDir = Path.GetDirectoryName(oldPath)!;
                    string newPath = Path.Combine(parentDir, newName);

                    try
                    {
                        bool isRoot = oldPath.TrimEnd('\\', '/')
                            .Equals(ProjectManager.CurrentProjectPath.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);

                        if (Directory.Exists(oldPath))
                        {
                            Directory.Move(oldPath, newPath);

                            UpdateOpenTabPaths(oldPath, newPath);

                            if (isRoot)
                            {
                                ProjectManager.CurrentProjectPath = newPath;
                            }
                        }
                        else if (File.Exists(oldPath))
                        {
                            File.Move(oldPath, newPath);

                            UpdateOpenTabPaths(oldPath, newPath);
                        }

                        RefreshSolutionExplorer();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error renaming: {ex.Message}", "GSCLab Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void UpdateOpenTabPaths(string oldPath, string newPath)
        {
            var allTabs = EditorTabs.Items.OfType<TabItem>().Concat(EditorTabsRight.Items.OfType<TabItem>());

            foreach (TabItem tab in allTabs)
            {
                string tabPath = tab.Tag?.ToString();
                if (string.IsNullOrEmpty(tabPath)) continue;

                if (tabPath.Equals(oldPath, StringComparison.OrdinalIgnoreCase))
                {
                    string newTabPath = newPath;
                    ApplyTabPath(tab, newTabPath);
                }
                else if (tabPath.StartsWith(oldPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    string relative = tabPath.Substring(oldPath.Length);
                    string newTabPath = newPath + relative;
                    ApplyTabPath(tab, newTabPath);
                }
            }
        }

        private void ApplyTabPath(TabItem tab, string newTabPath)
        {
            tab.Tag = newTabPath;

            if (tab.Content is ICSharpCode.AvalonEdit.TextEditor editor)
            {
                editor.Tag = newTabPath;
            }

            string header = tab.Header.ToString()!;
            bool hasUnsaved = header.EndsWith("*");
            string prefix = header.StartsWith("⚙️") ? "⚙️" : "📄";
            tab.Header = $"{prefix} {Path.GetFileName(newTabPath)}{(hasUnsaved ? "*" : "")}";
        }

        private void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            IInputElement element = SolutionTreeView.InputHitTest(e.GetPosition(SolutionTreeView));
            DependencyObject container = element as DependencyObject;

            while (container != null && !(container is TreeViewItem))
            {
                DependencyObject parent = null;

                if (container is Visual || container is System.Windows.Media.Media3D.Visual3D)
                {
                    parent = VisualTreeHelper.GetParent(container);
                }

                if (parent == null)
                {
                    parent = LogicalTreeHelper.GetParent(container);
                }

                container = parent;
            }
            if (container is TreeViewItem item)
            {
                item.Focus();
                item.IsSelected = true;
            }
        }

        private void SolutionTreeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (string.IsNullOrEmpty(ProjectManager.CurrentProjectPath))
            {
                e.Handled = true;
                return;
            }

            if (SolutionTreeView.SelectedItem is TreeViewItem selected && selected.Tag != null)
            {
                string path = selected.Tag.ToString();
                bool isDirectory = Directory.Exists(path);

                MenuAddGsc.Visibility = isDirectory ? Visibility.Visible : Visibility.Collapsed;
                MenuAddFolder.Visibility = isDirectory ? Visibility.Visible : Visibility.Collapsed;
                MenuSeparator.Visibility = isDirectory ? Visibility.Visible : Visibility.Collapsed;

                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        private List<string> GetExpandedPaths(ItemCollection items)
        {
            var expandedPaths = new List<string>();
            foreach (TreeViewItem item in items)
            {
                if (item.IsExpanded && item.Tag != null)
                {
                    expandedPaths.Add(item.Tag.ToString());
                }
                expandedPaths.AddRange(GetExpandedPaths(item.Items));
            }
            return expandedPaths;
        }

        private void CloseTabByFilePath(string filePath)
        {
            TabItem tabToRemove = null;
            TabControl parentControl = null;

            foreach (TabItem tab in EditorTabs.Items)
            {
                if (tab.Tag?.ToString() == filePath)
                {
                    tabToRemove = tab;
                    parentControl = EditorTabs;
                    break;
                }
            }

            if (tabToRemove == null)
            {
                foreach (TabItem tab in EditorTabsRight.Items)
                {
                    if (tab.Tag?.ToString() == filePath)
                    {
                        tabToRemove = tab;
                        parentControl = EditorTabsRight;
                        break;
                    }
                }
            }

            if (tabToRemove != null && parentControl != null)
            {
                parentControl.Items.Remove(tabToRemove);
                UpdateSplitViewVisibility();
            }
        }

        private void CloseTab_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement closeButton)
            {
                TabItem tabItem = FindParent<TabItem>(closeButton);

                if (tabItem != null)
                {
                    e.Handled = true;

                    if (tabItem.Content is ICSharpCode.AvalonEdit.TextEditor editor)
                    {
                        string filePath = tabItem.Tag?.ToString()!;

                        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                        {
                            string diskText = File.ReadAllText(filePath);

                            if (editor.Text != diskText)
                            {
                                var result = GscMessageBox.Show($"Save changes to '{Path.GetFileName(filePath)}'?", "UNSAVED CHANGES", MessageBoxButton.YesNo);

                                if (result == MessageBoxResult.Yes)
                                {
                                    editor.Save(filePath);
                                }
                                else if (result == MessageBoxResult.Cancel)
                                {
                                    return;
                                }
                            }
                        }
                    }

                    if (EditorTabs.Items.Contains(tabItem))
                    {
                        EditorTabs.Items.Remove(tabItem);
                    }
                    else if (EditorTabsRight.Items.Contains(tabItem))
                    {
                        EditorTabsRight.Items.Remove(tabItem);
                    }

                    UpdateSplitViewVisibility();

                    if (EditorTabs.Items.Count == 0 && EditorTabsRight.Items.Count == 0)
                    {
                        if (!EditorTabs.Items.Contains(tabExamples))
                        {
                            EditorTabs.Items.Add(tabExamples);
                            EditorTabs.SelectedItem = tabExamples;
                        }
                    }
                }
            }
        }
        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            if (child == null) return null;

            DependencyObject parentObject;

            if (child is Visual || child is System.Windows.Media.Media3D.Visual3D)
            {
                parentObject = VisualTreeHelper.GetParent(child) ?? LogicalTreeHelper.GetParent(child);
            }
            else
            {
                parentObject = LogicalTreeHelper.GetParent(child);
            }

            if (parentObject == null) return null;

            return parentObject is T parent ? parent : FindParent<T>(parentObject);
        }

        public async Task<byte[]> RequestCompile(string game, string scriptName, string code)
        {
            if (string.IsNullOrEmpty(Infrastructure.Session.Token))
            {
                throw new Exception("No active session. Please log in again.");
            }

            try
            {
                using (TcpClient client = new TcpClient("45.90.116.64", 8888))
                using (NetworkStream stream = client.GetStream())
                {
                    string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(code));

                    string cmd = $"COMPILE|{Infrastructure.Session.Token}|{game}|{scriptName}|{b64}<EOF>";

                    byte[] cmdData = Encoding.UTF8.GetBytes(cmd);
                    await stream.WriteAsync(cmdData, 0, cmdData.Length);

                    byte[] header = new byte[4];
                    int read = await stream.ReadAsync(header, 0, 4);
                    if (read < 4) return null;

                    string headerText = Encoding.UTF8.GetString(header);

                    if (headerText == "ERRO")
                    {
                        byte[] errorBuffer = new byte[1024];
                        int errorRead = await stream.ReadAsync(errorBuffer, 0, errorBuffer.Length);
                        string fullError = Encoding.UTF8.GetString(errorBuffer, 0, errorRead);
                        throw new Exception(fullError.Replace("|", ""));
                    }

                    int fileSize = BitConverter.ToInt32(header, 0);

                    byte[] fileData = new byte[fileSize];
                    int totalRead = 0;
                    while (totalRead < fileSize)
                    {
                        int r = await stream.ReadAsync(fileData, totalRead, fileSize - totalRead);
                        if (r == 0) break;
                        totalRead += r;
                    }

                    return fileData;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public bool CanCloseProject()
        {
            foreach (TabItem tab in EditorTabs.Items.OfType<TabItem>().ToList())
            {
                if (tab.Header.ToString().EndsWith("*"))
                {
                    EditorTabs.SelectedItem = tab;
                    string fileName = tab.Header.ToString().Replace("📄 ", "").Replace("*", "");

                    var result = GscMessageBox.Show($"File '{fileName}' has unsaved changes. Do you want to save before closing?", "UNSAVED CHANGES", MessageBoxButton.YesNo);

                    if (result == MessageBoxResult.Yes)
                    {
                        SaveProject(false);
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        #region Debug Logger

        private void ClearOutput_Click(object sender, RoutedEventArgs e) => DebugOutput.Clear();

        public void Log(string message)
        {
            Dispatcher.Invoke(() => {
                DebugOutput.AppendText(message + Environment.NewLine);

                DebugOutput.ScrollToEnd();
            });
        }

        #endregion

        #region AvalonEdit Popup Menu

        private void SetupEditorContextMenu(ICSharpCode.AvalonEdit.TextEditor editor)
        {
            var menu = new ContextMenu();

            var jumpItem = new MenuItem { Header = "Jump to Definition", InputGestureText = "F12" };
            jumpItem.Click += (s, e) => JumpToDefinition(editor);

            //var renameItem = new MenuItem { Header = "Change All Occurrences", InputGestureText = "Ctrl+R" };
            //renameItem.Click += (s, e) => RenameAllOccurrences(editor);
            //menu.Items.Add(renameItem);

            menu.Items.Add(jumpItem);
            menu.Items.Add(new Separator { Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)) });
            menu.Items.Add(new MenuItem { Header = "Copy", Command = ApplicationCommands.Copy });
            menu.Items.Add(new MenuItem { Header = "Paste", Command = ApplicationCommands.Paste });

            editor.PreviewMouseRightButtonDown += (s, e) => {
                var pos = editor.GetPositionFromPoint(e.GetPosition(editor));
                if (pos.HasValue)
                {
                    editor.TextArea.Caret.Offset = editor.Document.GetOffset(pos.Value.Location);
                }
            };

            editor.ContextMenu = menu;
        }
        private void JumpToDefinition(ICSharpCode.AvalonEdit.TextEditor currentEditor)
        {
            int startOffset = currentEditor.CaretOffset;
            int currentLine = currentEditor.Document.GetLineByOffset(startOffset).LineNumber;
            string word = GetWordAtOffset(currentEditor.Document, startOffset);

            if (string.IsNullOrEmpty(word) || Infrastructure.FunctionLibrary.IsKeyword(word)) return;

            Log($"^5[LOG] INFO: Attempting jump for '{word}'...");

            string pattern = $@"^\s*{Regex.Escape(word)}\s*\([\s\S]*?\)\s*\{{";

            var localMatches = Regex.Matches(currentEditor.Text, pattern, RegexOptions.Multiline);
            foreach (Match m in localMatches)
            {
                int matchLine = currentEditor.Document.GetLineByOffset(m.Index).LineNumber;

                if (matchLine != currentLine)
                {
                    Log($"^2[LOG] SUCCESS: Found definition for '{word}' locally on line {matchLine}.");
                    NavigateToMatch(currentEditor, m, word.Length);
                    return;
                }
            }

            string projectPath = ProjectManager.CurrentProjectPath;
            if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath)) return;

            var allFiles = Directory.GetFiles(projectPath, "*.gsc", SearchOption.AllDirectories);
            foreach (var file in allFiles)
            {
                if (currentEditor.Tag?.ToString() == file) continue;

                string content = File.ReadAllText(file);
                var remoteMatch = Regex.Match(content, pattern, RegexOptions.Multiline);

                if (remoteMatch.Success)
                {
                    Log($"^2[LOG] SUCCESS: Found '{word}' in {Path.GetFileName(file)}. Opening...");

                    var targetEditor = OpenGscEditorTab(file);

                    Dispatcher.BeginInvoke(new Action(() => {
                        var finalMatch = Regex.Match(targetEditor.Text, pattern, RegexOptions.Multiline);
                        if (finalMatch.Success)
                        {
                            NavigateToMatch(targetEditor, finalMatch, word.Length);
                        }
                    }), System.Windows.Threading.DispatcherPriority.Input);

                    return;
                }
            }

            Log($"^1[LOG] ERROR: Definition for '{word}' not found.");
        }
        private void NavigateToMatch(ICSharpCode.AvalonEdit.TextEditor editor, Match match, int wordLength)
        {
            editor.Focus();
            editor.TextArea.Caret.Offset = match.Index;
            editor.ScrollToLine(editor.Document.GetLineByOffset(match.Index).LineNumber);
            editor.Select(match.Index, wordLength);
        }
        private string GetWordAtOffset(TextDocument doc, int offset)
        {
            if (offset >= doc.TextLength) return string.Empty;

            int start = offset;
            while (start > 0 && IsGscLetter(doc.GetCharAt(start - 1))) start--;

            int end = offset;
            while (end < doc.TextLength && IsGscLetter(doc.GetCharAt(end))) end++;

            string fullWord = doc.GetText(start, end - start);

            if (fullWord.Contains("::"))
            {
                return fullWord.Split(new[] { "::" }, StringSplitOptions.None).Last();
            }

            return fullWord;
        }
        private bool IsGscLetter(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '\\' || c == ':';

        #endregion

        #region Drag_Drop

        private Point _startPoint;
        private TreeViewItem _draggedItem = null;
        private bool _isDragging = false;

        private void SolutionTreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(SolutionTreeView);
            _draggedItem = FindParent<TreeViewItem>(e.OriginalSource as DependencyObject);

            if (_draggedItem == null) return;
            _draggedItem.IsSelected = true;
            _draggedItem.Focus();

        }

        private void SolutionTreeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _draggedItem != null && !_isDragging)
            {
                Point currentPosition = e.GetPosition(SolutionTreeView);

                if (Math.Abs(currentPosition.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(currentPosition.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (_draggedItem.Tag == null) return;

                    string sourcePath = _draggedItem.Tag.ToString()!;

                    if (sourcePath == ProjectManager.CurrentProjectPath) return;

                    _isDragging = true;
                    DataObject dragData = new DataObject("GSCFile", sourcePath);

                    DragDrop.DoDragDrop(_draggedItem, dragData, DragDropEffects.Move);

                    _isDragging = false;
                    _draggedItem = null;
                }
            }
        }

        private void SolutionTreeView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _draggedItem = null;
            _isDragging = false;
        }

        private void SolutionTreeView_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("GSCFile"))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }
            e.Effects = DragDropEffects.Move;
        }

        private void SolutionTreeView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("GSCFile"))
            {
                string sourcePath = (string)e.Data.GetData("GSCFile");

                IInputElement element = SolutionTreeView.InputHitTest(e.GetPosition(SolutionTreeView));
                TreeViewItem targetItem = FindParent<TreeViewItem>(element as DependencyObject);

                if (targetItem != null && targetItem.Tag != null)
                {
                    string targetPath = targetItem.Tag.ToString()!;

                    if (File.Exists(targetPath))
                    {
                        targetPath = Path.GetDirectoryName(targetPath)!;
                    }

                    string fileName = Path.GetFileName(sourcePath);
                    string destinationPath = Path.Combine(targetPath, fileName);

                    try
                    {
                        if (sourcePath != destinationPath)
                        {
                            string targetFolderName = new DirectoryInfo(targetPath).Name;

                            var result = GscMessageBox.Show($"Are you sure you want to move '{fileName}' to '{targetFolderName}'?", "Confirm Move", MessageBoxButton.YesNo);

                            if (result == MessageBoxResult.Yes)
                            {
                                if (File.Exists(sourcePath))
                                {
                                    CloseTabByFilePath(sourcePath);
                                    File.Move(sourcePath, destinationPath);
                                }
                                else if (Directory.Exists(sourcePath))
                                {
                                    Directory.Move(sourcePath, destinationPath);
                                }

                                RefreshSolutionExplorer();
                                Log($"^2[LOG] MOVED: {fileName} -> {targetFolderName}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error moving item: {ex.Message}", "GSCLab", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        #endregion

        private void btnCheckSyntax_Click(object sender, RoutedEventArgs e)
        {
            SaveProject(false);

            try
            {
                string projectPath = Infrastructure.ProjectManager.CurrentProjectPath;
                if (string.IsNullOrEmpty(projectPath)) return;

                string[] gscFiles = Directory.GetFiles(projectPath, "*.gsc", SearchOption.AllDirectories);
                var allProjectFunctions = new List<GscCompletionData>();
                foreach (var file in gscFiles)
                {
                    string content = File.ReadAllText(file);

                    var defs = Regex.Matches(content, @"^\s*([a-zA-Z_]\w*)\s*\(", RegexOptions.Multiline);

                    foreach (var name in GetRealFunctionDefinitions(content))
                    {
                        allProjectFunctions.Add(new GscCompletionData(name, "Local", ""));
                    }
                }

                StringBuilder masterBuilder = new StringBuilder();

                foreach (string file in gscFiles)
                {
                    string content = File.ReadAllText(file);
                    string fileName = Path.GetFileName(file);

                    content = PreProcessGscMemoryCalls(content);

                    var syntaxCheck = Infrastructure.SyntaxChecker.Validate(content, fileName);
                    if (!syntaxCheck.IsValid)
                    {
                        Log(syntaxCheck.ErrorMessage);
                        return;
                    }

                    bool isLinkerValid = Infrastructure.FunctionLibrary.ValidateCodeBeforeInject(content, allProjectFunctions, fileName);

                    if (!isLinkerValid)
                    {
                        Log("^1[LOG] FAILED: Linker check failed. Unknown functions found.");
                        return;
                    }

                    masterBuilder.AppendLine($"// File: {fileName}");
                    masterBuilder.AppendLine(content);
                    masterBuilder.AppendLine();
                }

                Log("^2[LOG] SUCCESS: No script errors found.");
            }
            catch (Exception ex)
            {

            }
        }

        #region PreProcessGSCMemoryCalls

        private string PreProcessGscMemoryCalls(string code)
        {
            string[] memoryFunctions = { "WriteByte", "WriteBytes", "WriteInt", "WriteInts", "WriteShort", "WriteShorts", "WriteString", "WriteFloat", "WriteFloats", "ReadByte", "ReadBytes", "ReadInt", "ReadInts", "ReadShort", "ReadShorts", "ReadString", "ReadFloat", "ReadFloats", "RPC" };

            string functionPattern = string.Join("|", memoryFunctions);
            string pattern = $@"\b({functionPattern})\s*\(([^)]+)\)";

            return Regex.Replace(code, pattern, m =>
            {
                string funcName = m.Groups[1].Value;
                string argsRaw = m.Groups[2].Value;

                string[] args = Regex.Split(argsRaw, @",(?=(?:[^""]*""[^""]*"")*[^""]*$)");

                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i].Trim();

                    if (arg.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        args[i] = $"\"{arg.Substring(2)}\"";
                    }
                    else if (Regex.IsMatch(arg, @"^\d+$"))
                    {
                        args[i] = $"\"{arg}\"";
                    }
                }

                return $"{funcName}({string.Join(", ", args)})";
            });
        }

        #endregion

        #region Sleek Search & Replace Logic

        private SearchPanel GetPanelFromSender(object sender)
        {
            if (sender is DependencyObject d)
            {
                return FindParent<SearchPanel>(d);
            }
            return null;
        }

        private void SearchInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                var panel = GetPanelFromSender(tb);
                if (panel != null) panel.SearchPattern = tb.Text;
            }
        }

        private void FindNext_Click(object sender, RoutedEventArgs e)
        {
            GetPanelFromSender(sender)?.FindNext();
        }

        private void FindPrev_Click(object sender, RoutedEventArgs e)
        {
            GetPanelFromSender(sender)?.FindPrevious();
        }

        private void CloseSearch_Click(object sender, RoutedEventArgs e)
        {
            GetPanelFromSender(sender)?.Close();
        }

        private void ReplaceNext_Click(object sender, RoutedEventArgs e)
        {
            var panel = GetPanelFromSender(sender);
            if (panel == null || !_editorMap.TryGetValue(panel, out var editor)) return;

            string searchText = panel.SearchPattern;
            if (string.IsNullOrEmpty(searchText)) return;

            var replaceTextBox = panel.Template.FindName("ReplaceInput", panel) as TextBox;
            string replaceText = replaceTextBox?.Text ?? "";

            int match = editor.Document.IndexOf(searchText, editor.CaretOffset, editor.Document.TextLength - editor.CaretOffset, StringComparison.OrdinalIgnoreCase);

            if (match != -1)
            {
                editor.Document.Replace(match, searchText.Length, replaceText);
                panel.FindNext();
            }
        }

        private void ReplaceAll_Click(object sender, RoutedEventArgs e)
        {
            var panel = GetPanelFromSender(sender);
            if (panel == null || !_editorMap.TryGetValue(panel, out var editor)) return;

            string searchText = panel.SearchPattern;
            if (string.IsNullOrEmpty(searchText)) return;

            var replaceTextBox = panel.Template.FindName("ReplaceInput", panel) as TextBox;
            string replaceText = replaceTextBox?.Text ?? "";
            var matches = Regex.Matches(editor.Text, Regex.Escape(searchText), RegexOptions.IgnoreCase);

            if (matches.Count == 0) return;

            using (editor.Document.RunUpdate())
            {
                for (int i = matches.Count - 1; i >= 0; i--)
                {
                    var match = matches[i];
                    editor.Document.Replace(match.Index, match.Length, replaceText);
                }
            }
        }

        #endregion

        #region Bracket Matching Helper

        private int FindMatchingBracket(TextDocument document, int offset, char openBracket, char closeBracket, int direction)
        {
            int nestingLevel = 0;
            int currentOffset = offset + direction;

            while (currentOffset >= 0 && currentOffset < document.TextLength)
            {
                char c = document.GetCharAt(currentOffset);

                if (c == openBracket)
                {
                    nestingLevel++;
                }
                else if (c == closeBracket)
                {
                    if (nestingLevel == 0) return currentOffset;
                    nestingLevel--;
                }

                currentOffset += direction;
            }
            return -1;
        }

        #endregion

        #region Split View Logic

        private Point _dragStartPoint;
        private bool _mouseDownOnHeader = false;

        private void TabItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is TabItem tabItem)
            {
                _dragStartPoint = e.GetPosition(null);
                _mouseDownOnHeader = IsActualHeaderClick(e.OriginalSource as DependencyObject, tabItem);
            }
        }

        private bool IsActualHeaderClick(DependencyObject clicked, TabItem tabItem)
        {
            DependencyObject current = clicked;

            while (current != null && current != tabItem)
            {
                if (current is System.Windows.Controls.Primitives.ScrollBar || current is System.Windows.Controls.Primitives.Thumb || current is System.Windows.Controls.Primitives.RepeatButton || current is ICSharpCode.AvalonEdit.TextEditor || current is ScrollViewer)
                {
                    return false;
                }

                current = (current is Visual || current is System.Windows.Media.Media3D.Visual3D) ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current);
            }

            return current == tabItem;
        }

        private void TabItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || !_mouseDownOnHeader) return;

            if (sender is TabItem tabItem)
            {
                if (tabItem.Name == "tabExamples" || tabItem.Header.ToString().Contains("config.gl"))
                    return;

                Point currentMousePos = e.GetPosition(null);
                Vector diff = _dragStartPoint - currentMousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.Move);
                    _mouseDownOnHeader = false;
                }
            }
        }

        private void TabItem_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _mouseDownOnHeader = false;
        }

        private void MainEditorGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(TabItem)) is TabItem draggedTab)
            {
                TabControl sourceTabControl = draggedTab.Parent as TabControl;
                if (sourceTabControl == null) return;

                Grid mainGrid = sender as Grid;
                Point dropPosition = e.GetPosition(mainGrid);

                TabControl targetTabControl = (dropPosition.X > mainGrid.ActualWidth / 2)
                    ? EditorTabsRight
                    : EditorTabs;

                if (sourceTabControl != targetTabControl)
                {
                    sourceTabControl.Items.Remove(draggedTab);
                    targetTabControl.Items.Add(draggedTab);
                    targetTabControl.SelectedItem = draggedTab;

                    if (draggedTab.Content is ICSharpCode.AvalonEdit.TextEditor editor)
                    {
                        var oldEntry = _editorMap.FirstOrDefault(x => x.Value == editor);
                        bool wasOpen = oldEntry.Key != null && !oldEntry.Key.IsClosed;
                        string previousPattern = oldEntry.Key?.SearchPattern;

                        if (oldEntry.Key != null)
                        {
                            oldEntry.Key.Uninstall();
                            _editorMap.Remove(oldEntry.Key);
                        }

                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            var newPanel = ICSharpCode.AvalonEdit.Search.SearchPanel.Install(editor);
                            _editorMap[newPanel] = editor;

                            if (!string.IsNullOrEmpty(previousPattern))
                                newPanel.SearchPattern = previousPattern;

                            if (wasOpen)
                                newPanel.Open();

                        }), System.Windows.Threading.DispatcherPriority.Loaded);
                    }

                    UpdateSplitViewVisibility();
                }
            }
        }

        private void UpdateSplitViewVisibility()
        {
            if (EditorTabsRight.Items.Count > 0)
            {
                LeftEditorColumn.Width = new GridLength(1, GridUnitType.Star);
                RightEditorColumn.Width = new GridLength(1, GridUnitType.Star);

                EditorSplitter.Visibility = Visibility.Visible;
                EditorTabsRight.Visibility = Visibility.Visible;
            }
            else
            {
                LeftEditorColumn.Width = new GridLength(1, GridUnitType.Star);
                RightEditorColumn.Width = new GridLength(0);

                EditorSplitter.Visibility = Visibility.Collapsed;
                EditorTabsRight.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region Discord

        private string GetIcon(string gameName)
        {
            switch (gameName)
            {
                case "cod2": return "iw2_logo";
                case "mw1": return "iw3_logo";
                case "qos": return "t3_logo";
                case "waw": return "t4_logo";
                case "mw2": return "iw4_logo";
                case "bo1": return "t5_logo";
                case "mw3": return "iw5_logo";
                case "bo2": return "t6_logo";
                case "ghosts": return "iw6_logo";
                case "aw": return "s1_logo";
                case "bo3": return "t7_logo";
                case "iw": return "iw7_logo";
                case "mwr": return "h1_logo";
                case "ww2": return "s2_logo";
                case "bo4": return "t8_logo";
                case "mw2019": return "iw8_logo";
                case "bocw": return "t9_logo";
                case "vangaurd": return "s4_logo";
                case "mw22": return "iw9_logo";
                case "mw323": return "jup_logo";
                case "bo6": return "t10_logo";
                case "bo7": return "sat_logo";
                default: return "null";
            }
        }

        private string GetIconText(string gameName)
        {
            switch (gameName)
            {
                case "cod2": return "COD2";
                case "mw1": return "COD4";
                case "qos": return "Quantum of Solace";
                case "waw": return "World At War";
                case "mw2": return "Modern Warfare 2";
                case "bo1": return "Black Ops";
                case "mw3": return "Modern Warfare 3";
                case "bo2": return "Black Ops II";
                case "ghosts": return "Ghosts";
                case "aw": return "Advanced Warfare";
                case "bo3": return "Black Ops III";
                case "iw": return "Infinte Warfare";
                case "mwr": return "Modern Warfare Remastered";
                case "ww2": return "World War II";
                case "bo4": return "Black Ops 4";
                case "mw2019": return "Modfern Warfare 2019";
                case "bocw": return "Black Ops Cold War";
                case "vangaurd": return "Vanguard";
                case "mw22": return "Modern Warfare 2 2022";
                case "mw323": return "Modern Warfare 3 2023";
                case "bo6": return "Black Ops 6";
                case "bo7": return "Black Ops 7";
                default: return "null";
            }
        }

        private void EditorTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource != EditorTabs && e.OriginalSource != EditorTabsRight) return;

            TabControl activeTabControl = sender as TabControl;
            if (activeTabControl?.SelectedItem is TabItem activeTab)
            {

                string projectName = string.IsNullOrEmpty(ProjectManager.CurrentProjectPath) ? "Standalone File" : System.IO.Path.GetFileName(ProjectManager.CurrentProjectPath);

                string fileName = activeTab.Header.ToString().Replace("📄 ", "").Replace("*", "");

                if (fileName == "Examples")
                {
                    Discord.UpdatePresence("Viewing Examples", "GSCLab Documentation", "gsclab_logo", "GSCLab IDE");
                }
                else
                {
                    Discord.UpdatePresence($"Editing {fileName}", $"Project: {projectName}", GetIcon(ProjectManager.ActiveGame), GetIconText(ProjectManager.ActiveGame));
                }
            }
            else if (EditorTabs.Items.Count == 0 && EditorTabsRight.Items.Count == 0)
            {
                Discord.SetIdlePresence();
            }
        }

        #endregion

        #region Auto Comments

        private void ToggleComments(ICSharpCode.AvalonEdit.TextEditor editor)
        {
            var doc = editor.Document;
            if (doc == null) return;

            int startLine = doc.GetLineByOffset(editor.SelectionStart).LineNumber;
            int endLine = doc.GetLineByOffset(editor.SelectionStart + editor.SelectionLength).LineNumber;

            if (editor.SelectionLength > 0 && editor.SelectionStart + editor.SelectionLength == doc.GetLineByNumber(endLine).Offset)
            {
                endLine--;
            }

            bool shouldUncomment = true;
            for (int i = startLine; i <= endLine; i++)
            {
                var lineText = doc.GetText(doc.GetLineByNumber(i));

                if (!string.IsNullOrWhiteSpace(lineText) && !lineText.TrimStart().StartsWith("//"))
                {
                    shouldUncomment = false;
                    break;
                }
            }

            doc.BeginUpdate();
            try
            {
                for (int i = startLine; i <= endLine; i++)
                {
                    var line = doc.GetLineByNumber(i);
                    string lineText = doc.GetText(line);

                    if (shouldUncomment)
                    {
                        int commentIndex = lineText.IndexOf("//");
                        if (commentIndex != -1)
                        {
                            doc.Remove(line.Offset + commentIndex, 2);
                        }
                    }
                    else
                    {
                        int insertOffset = 0;
                        while (insertOffset < lineText.Length && char.IsWhiteSpace(lineText[insertOffset]))
                        {
                            insertOffset++;
                        }

                        doc.Insert(line.Offset + insertOffset, "//");
                    }
                }
            }
            finally
            {
                doc.EndUpdate();
            }
        }

        #endregion

        #region Sanitize Gsc Code

        class PreprocessorBlock
        {
            public bool CurrentMatch { get; set; }
            public bool MatchedAny { get; set; }
            public bool ParentIgnoring { get; set; }
            public bool Ignoring => ParentIgnoring || !CurrentMatch;
        }

        private string SanitizeGscCode(string rawCode, string codename, string activeGame, string activeMode)
        {
            if (string.IsNullOrWhiteSpace(rawCode)) return string.Empty;

            var lines = rawCode.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
            var cleanLines = new List<string>();

            var config = Infrastructure.ProjectManager.ConfigUI;
            string platform = config?.SelectedPlatform == "unknown" ? Infrastructure.ProjectManager.ActivePlatform : (config?.SelectedPlatform ?? "");

            var validIdentifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(activeGame)) validIdentifiers.Add(activeGame);
            if (!string.IsNullOrEmpty(codename)) validIdentifiers.Add(codename);
            if (!string.IsNullOrEmpty(platform)) validIdentifiers.Add(platform);
            if (!string.IsNullOrEmpty(activeMode)) validIdentifiers.Add(activeMode);

            string mappedCode = activeGame?.ToLower() ?? "";
            if (mappedCode == "aw" || codename?.ToLower() == "aw") { validIdentifiers.Add("aw"); validIdentifiers.Add("s1"); }
            else if (mappedCode == "mwr" || codename?.ToLower() == "mwr") { validIdentifiers.Add("mwr"); validIdentifiers.Add("h1"); }
            else if (mappedCode == "ghosts" || codename?.ToLower() == "ghosts") { validIdentifiers.Add("ghosts"); validIdentifiers.Add("iw6"); }
            else if (mappedCode == "mw2cr" || codename?.ToLower() == "mw2cr") { validIdentifiers.Add("mw2cr"); validIdentifiers.Add("h2"); }
            else if (mappedCode == "ww2" || codename?.ToLower() == "ww2") { validIdentifiers.Add("ww2"); validIdentifiers.Add("s2"); }
            else if (mappedCode == "mw3" || codename?.ToLower() == "mw3") { validIdentifiers.Add("mw3"); validIdentifiers.Add("iw5"); }
            else if (mappedCode == "iw" || codename?.ToLower() == "iw") { validIdentifiers.Add("iw"); validIdentifiers.Add("iw7"); }
            else if (mappedCode == "vanguard" || codename?.ToLower() == "vanguard") { validIdentifiers.Add("vanguard"); validIdentifiers.Add("s4"); }
            else if (mappedCode == "mw22" || codename?.ToLower() == "mw22") { validIdentifiers.Add("mw22"); validIdentifiers.Add("iw9"); }
            else if (mappedCode == "mw2019" || codename?.ToLower() == "mw2019") { validIdentifiers.Add("mw2019"); validIdentifiers.Add("iw8"); }

            var stack = new Stack<PreprocessorBlock>();

            string ExtractCondition(string trimmedLine, string prefix)
            {
                string cond = trimmedLine.Substring(prefix.Length).Trim();
                if (cond.StartsWith("(")) cond = cond.Substring(1).Trim();
                if (cond.EndsWith(")")) cond = cond.Substring(0, cond.Length - 1).Trim();
                return cond;
            }

            bool EvaluateCondition(string condition)
            {
                string originalCond = condition;

                condition = condition.Replace("DEFINED", "")
                                     .Replace("IFDEF", "")
                                     .Replace("IFNDEF", "")
                                     .Replace("IF", "")
                                     .Trim();

                if (condition.StartsWith("(") && condition.EndsWith(")"))
                    condition = condition.Substring(1, condition.Length - 2).Trim();

                bool negate = condition.StartsWith("!");
                if (negate)
                    condition = condition.Substring(1).Trim();

                string[] andParts = condition.Split(["&&"], StringSplitOptions.RemoveEmptyEntries);
                bool finalResult = true;

                foreach (var andPart in andParts)
                {
                    string cleanAnd = andPart.Replace("(", " ").Replace(")", " ");
                    string[] orParts = cleanAnd.Split(["||"], StringSplitOptions.RemoveEmptyEntries);
                    bool orResult = false;

                    foreach (var orPart in orParts)
                    {
                        string p = orPart.Trim();
                        if (string.IsNullOrEmpty(p))
                            continue;

                        foreach (var identifier in validIdentifiers)
                        {
                            if (p.Equals(identifier, StringComparison.OrdinalIgnoreCase))
                            {
                                orResult = true;
                                break;
                            }
                        }

                        if (orResult)
                            break;
                    }
                    if (!orResult)
                    {
                        finalResult = false;
                        break;
                    }
                }

                bool result = negate ? !finalResult : finalResult;

                return result;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                string trimmed = line.TrimStart();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    bool isCurrentlyIgnoring = stack.Count > 0 && stack.Peek().Ignoring;
                    if (!isCurrentlyIgnoring)
                        cleanLines.Add(line);
                    continue;
                }

                string upper = trimmed.ToUpper();

                if (upper.StartsWith("#IF"))
                {
                    bool isIfNDef = upper.StartsWith("#IFNDEF");
                    string prefix = upper.StartsWith("#IFNDEF") ? "#IFNDEF" :
                        (upper.StartsWith("#IFDEF") ? "#IFDEF" : "#IF");

                    string cond = ExtractCondition(trimmed, prefix);
                    bool match = EvaluateCondition(cond);

                    if (isIfNDef)
                        match = !match;

                    bool parentIgnoring = stack.Count > 0 && stack.Peek().Ignoring;

                    stack.Push(new PreprocessorBlock
                    {
                        CurrentMatch = match,
                        MatchedAny = match,
                        ParentIgnoring = parentIgnoring
                    });
                }
                else if (upper.StartsWith("#ELIF"))
                {
                    if (stack.Count == 0) continue;

                    var currentBlock = stack.Peek();
                    if (currentBlock.MatchedAny)
                        currentBlock.CurrentMatch = false;
                    else
                    {
                        string prefix = upper.StartsWith("#ELIFDEF") ? "#ELIFDEF" :
                            (upper.StartsWith("#ELIFNDEF") ? "#ELIFNDEF" : "#ELIF");

                        string cond = ExtractCondition(trimmed, prefix);
                        bool match = EvaluateCondition(cond);

                        currentBlock.CurrentMatch = match;
                        if (match)
                            currentBlock.MatchedAny = true;
                    }
                }
                else if (upper.StartsWith("#ELSE"))
                {
                    if (stack.Count == 0) continue;

                    var currentBlock = stack.Peek();
                    currentBlock.CurrentMatch = !currentBlock.MatchedAny;
                    currentBlock.MatchedAny = true;
                }
                else if (upper.StartsWith("#ENDIF"))
                {
                    if (stack.Count > 0)
                        stack.Pop();
                }
                else
                {
                    bool isIgnoring = stack.Count > 0 && stack.Peek().Ignoring;
                    if (!isIgnoring)
                        cleanLines.Add(line);
                }
            }

            return string.Join(Environment.NewLine, cleanLines);
        }

        #endregion

        private void SearchBox_PreviewDrag(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }
    }

    public class CoDColorTransformer : DocumentColorizingTransformer
    {
        protected override void ColorizeLine(DocumentLine line)
        {
            int lineStart = line.Offset;
            string text = CurrentContext.Document.GetText(line);

            var colors = new Dictionary<string, SolidColorBrush> {
                { "^1", new SolidColorBrush(Color.FromRgb(255, 75, 75)) },
                { "^2", new SolidColorBrush(Color.FromRgb(75, 181, 67)) },
                { "^3", new SolidColorBrush(Color.FromRgb(238, 210, 2)) },
                { "^5", new SolidColorBrush(Color.FromRgb(0, 255, 255)) }
            };

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '^' && i + 1 < text.Length)
                {
                    string tag = text.Substring(i, 2);
                    if (colors.ContainsKey(tag))
                    {
                        ChangeLinePart(lineStart + i, lineStart + i + 2, (VisualLineElement element) => {
                            element.TextRunProperties.SetForegroundBrush(Brushes.Transparent);
                        });

                        int colonIndex = text.IndexOf(':', i + 2);
                        int nextTag = text.IndexOf('^', i + 2);

                        int endOfColor;
                        if (colonIndex != -1 && (nextTag == -1 || colonIndex < nextTag))
                            endOfColor = colonIndex + 1;
                        else
                            endOfColor = (nextTag == -1) ? text.Length : nextTag;

                        ChangeLinePart(lineStart + i + 2, lineStart + endOfColor, (VisualLineElement element) => {
                            element.TextRunProperties.SetForegroundBrush(colors[tag]);
                        });

                        i = endOfColor - 1;
                    }
                }
            }
        }
    }
}