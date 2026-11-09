using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media;

namespace GSCLab.Infrastructure
{
    public class GscConditionalColorizer : DocumentColorizingTransformer
    {
        private class TextRange { public int Start; public int End; }

        private List<TextRange> _inactiveBlocks = new List<TextRange>();
        private List<int> _conditionLinesOffsets = new List<int>();

        class PreprocessorBlock
        {
            public bool CurrentMatch { get; set; }
            public bool MatchedAny { get; set; }
            public bool ParentIgnoring { get; set; }
            public bool Ignoring => ParentIgnoring || !CurrentMatch;
            public int BlockStartOffset { get; set; }
        }

        public void UpdateBlocks(TextDocument document)
        {
            _inactiveBlocks.Clear();
            _conditionLinesOffsets.Clear();

            var config = Infrastructure.ProjectManager.ConfigUI;
            string rawGame = config.SelectedGameCode == "unknown" ? ProjectManager.ActiveGame : config.SelectedGameCode;
            string platform = config.SelectedPlatform == "unknown" ? ProjectManager.ActivePlatform : config.SelectedPlatform;
            string mode = config.SelectedModeCode == "unknown" ? ProjectManager.ActiveMode : config.SelectedModeCode;
            string engineCode = config.SelectedGameCode == "unknown" ? ProjectManager.ActiveGame : config.SelectedGameCode;

            var validIdentifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(rawGame)) validIdentifiers.Add(rawGame);
            if (!string.IsNullOrEmpty(engineCode)) validIdentifiers.Add(engineCode);

            string mappedCode = rawGame?.ToLower() ?? "";
            if (mappedCode == "aw" || engineCode?.ToLower() == "aw") { validIdentifiers.Add("aw"); validIdentifiers.Add("s1"); }
            else if (mappedCode == "mwr" || engineCode?.ToLower() == "mwr") { validIdentifiers.Add("mwr"); validIdentifiers.Add("h1"); }
            else if (mappedCode == "ghosts" || engineCode?.ToLower() == "ghosts") { validIdentifiers.Add("ghosts"); validIdentifiers.Add("iw6"); }
            else if (mappedCode == "mw2cr" || engineCode?.ToLower() == "mw2cr") { validIdentifiers.Add("mw2cr"); validIdentifiers.Add("h2"); }
            else if (mappedCode == "ww2" || engineCode?.ToLower() == "ww2") { validIdentifiers.Add("ww2"); validIdentifiers.Add("s2"); }
            else if (mappedCode == "mw3" || engineCode?.ToLower() == "mw3") { validIdentifiers.Add("mw3"); validIdentifiers.Add("iw5"); }
            else if (mappedCode == "iw" || engineCode?.ToLower() == "iw") { validIdentifiers.Add("iw"); validIdentifiers.Add("iw7"); }
            else if (mappedCode == "vanguard" || engineCode?.ToLower() == "vanguard") { validIdentifiers.Add("vanguard"); validIdentifiers.Add("s4"); }
            else if (mappedCode == "mw22" || engineCode?.ToLower() == "mw22") { validIdentifiers.Add("mw22"); validIdentifiers.Add("iw9"); }
            else if (mappedCode == "mw2019" || engineCode?.ToLower() == "mw2019") { validIdentifiers.Add("mw2019"); validIdentifiers.Add("iw8"); }

            if (!string.IsNullOrEmpty(platform)) validIdentifiers.Add(platform);
            if (!string.IsNullOrEmpty(mode)) validIdentifiers.Add(mode);

            Debug.WriteLine($"Valid Identifiers Match List: [{string.Join(", ", validIdentifiers)}] | platform: {platform} | mode: {mode}");

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
                condition = condition.Replace("DEFINED", "")
                                     .Replace("IFDEF", "")
                                     .Replace("IFNDEF", "")
                                     .Replace("IF", "")
                                     .Trim();

                if (condition.StartsWith("(") && condition.EndsWith(")"))
                {
                    condition = condition.Substring(1, condition.Length - 2).Trim();
                }

                bool negate = condition.StartsWith("!");
                if (negate) condition = condition.Substring(1).Trim();

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
                        if (string.IsNullOrEmpty(p)) continue;

                        foreach (var identifier in validIdentifiers)
                        {
                            if (p.Equals(identifier, StringComparison.OrdinalIgnoreCase))
                            {
                                orResult = true;
                                break;
                            }
                        }

                        if (orResult) break;
                    }

                    if (!orResult)
                    {
                        finalResult = false;
                        break;
                    }
                }

                return negate ? !finalResult : finalResult;
            }

            // Track individual segments of conditional blocks (if / elif / else)
            int currentSegmentStart = -1;
            bool currentSegmentIsIgnoring = false;

            void CloseCurrentSegment(int endOffset)
            {
                if (currentSegmentStart != -1 && currentSegmentIsIgnoring)
                {
                    _inactiveBlocks.Add(new TextRange { Start = currentSegmentStart, End = endOffset });
                }
                currentSegmentStart = -1;
            }

            foreach (var line in document.Lines)
            {
                string text = document.GetText(line).TrimStart();
                if (string.IsNullOrWhiteSpace(text)) continue;

                string upper = text.ToUpper();

                if (upper.StartsWith("#IF"))
                {
                    _conditionLinesOffsets.Add(line.Offset);

                    bool isIfNDef = upper.StartsWith("#IFNDEF");
                    string prefix = upper.StartsWith("#IFNDEF") ? "#IFNDEF" : (upper.StartsWith("#IFDEF") ? "#IFDEF" : "#IF");

                    string cond = ExtractCondition(text, prefix);
                    bool match = EvaluateCondition(cond);
                    if (isIfNDef) match = !match;

                    bool parentIgnoring = stack.Count > 0 && stack.Peek().Ignoring;

                    stack.Push(new PreprocessorBlock
                    {
                        CurrentMatch = match,
                        MatchedAny = match,
                        ParentIgnoring = parentIgnoring,
                        BlockStartOffset = line.EndOffset
                    });

                    currentSegmentStart = line.EndOffset;
                    currentSegmentIsIgnoring = parentIgnoring || !match;
                }
                else if (upper.StartsWith("#ELIF"))
                {
                    _conditionLinesOffsets.Add(line.Offset);

                    if (stack.Count > 0)
                    {
                        CloseCurrentSegment(line.Offset);

                        var currentBlock = stack.Peek();

                        if (currentBlock.MatchedAny)
                        {
                            currentBlock.CurrentMatch = false;
                        }
                        else
                        {
                            string prefix = upper.StartsWith("#ELIFDEF") ? "#ELIFDEF" : (upper.StartsWith("#ELIFNDEF") ? "#ELIFNDEF" : "#ELIF");
                            string cond = ExtractCondition(text, prefix);
                            bool match = EvaluateCondition(cond);

                            currentBlock.CurrentMatch = match;
                            if (match) currentBlock.MatchedAny = true;
                        }

                        currentSegmentStart = line.EndOffset;
                        currentSegmentIsIgnoring = currentBlock.Ignoring;
                    }
                }
                else if (upper.StartsWith("#ELSE"))
                {
                    _conditionLinesOffsets.Add(line.Offset);

                    if (stack.Count > 0)
                    {
                        CloseCurrentSegment(line.Offset);

                        var currentBlock = stack.Peek();
                        currentBlock.CurrentMatch = !currentBlock.MatchedAny;
                        currentBlock.MatchedAny = true;

                        currentSegmentStart = line.EndOffset;
                        currentSegmentIsIgnoring = currentBlock.Ignoring;
                    }
                }
                else if (upper.StartsWith("#ENDIF"))
                {
                    _conditionLinesOffsets.Add(line.Offset);

                    if (stack.Count > 0)
                    {
                        CloseCurrentSegment(line.Offset);
                        stack.Pop();
                    }
                }
            }

            // Handle any unclosed blocks gracefully
            while (stack.Count > 0)
            {
                CloseCurrentSegment(document.TextLength);
                stack.Pop();
            }
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            if (_conditionLinesOffsets.Contains(line.Offset))
            {
                ChangeLinePart(line.Offset, line.EndOffset, element =>
                {
                    element.TextRunProperties.SetForegroundBrush(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A9A9A9")));
                });
                return;
            }

            foreach (var block in _inactiveBlocks)
            {
                if (line.Offset >= block.Start && line.EndOffset <= block.End)
                {
                    ChangeLinePart(line.Offset, line.EndOffset, element =>
                    {
                        element.TextRunProperties.SetForegroundBrush(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555"))); // Ghost gray
                    });
                    return;
                }
            }
        }
    }
}