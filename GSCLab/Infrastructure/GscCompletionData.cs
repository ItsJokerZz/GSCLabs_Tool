using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Windows.Media;

namespace GSCLab.Infrastructure
{
    public class GscCompletionData : ICompletionData
    {
        public string Text { get; private set; }
        public object Content => this;
        public object Description { get; private set; }
        public string Icon { get; private set; }
        public ImageSource Image => null!;
        public double Priority => 0;
        public GscCompletionData(string text, string description, string icon = "📄")
        {
            this.Text = text;
            this.Description = description;
            this.Icon = icon;
        }
        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, this.Text);
        }
    }
}