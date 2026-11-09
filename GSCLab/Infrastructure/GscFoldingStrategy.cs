using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Document;
using System.Collections.Generic;

namespace GSCLab.Infrastructure
{
    public class GscFoldingStrategy
    {
        public void UpdateFoldings(FoldingManager manager, TextDocument document)
        {
            var foldings = CreateNewFoldings(document, out int firstErrorOffset);
            manager.UpdateFoldings(foldings, firstErrorOffset);
        }

        public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
        {
            firstErrorOffset = -1;
            var newFoldings = new List<NewFolding>();
            var startOffsets = new Stack<int>();
            var startNames = new Stack<string>();

            for (int i = 1; i <= document.LineCount; i++)
            {
                var line = document.GetLineByNumber(i);
                string text = document.GetText(line).Trim();

                if (text.StartsWith("#region"))
                {
                    startOffsets.Push(line.Offset);
                    string name = text.Length > 8 ? text.Substring(8).Trim() : "Region";
                    startNames.Push(name);
                }
                else if (text.StartsWith("#endregion") && startOffsets.Count > 0)
                {
                    int start = startOffsets.Pop();
                    string name = startNames.Pop();

                    newFoldings.Add(new NewFolding(start, line.EndOffset)
                    {
                        Name = name
                    });

                    new NewFolding(start, line.EndOffset)
                    {
                        Name = "  " + name + "  ",
                        DefaultClosed = false
                    };
                }
            }

            newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
            return newFoldings;
        }
    }
}
