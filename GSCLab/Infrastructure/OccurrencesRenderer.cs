using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows.Media;

namespace GSCLab.Infrastructure
{
    public class OccurrencesRenderer : IBackgroundRenderer
    {
        private readonly TextEditor _editor;
        public string WordToHighlight { get; set; }
        public KnownLayer Layer => KnownLayer.Selection;
        public OccurrencesRenderer(TextEditor editor)
        {
            _editor = editor;
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (string.IsNullOrWhiteSpace(WordToHighlight) || textView == null || textView.VisualLines.Count == 0)
                return;

            var document = _editor.Document;
            string text = document.Text;

            var brush = new SolidColorBrush(Color.FromArgb(80, 0, 122, 204));
            var pen = new Pen(new SolidColorBrush(Color.FromRgb(0, 122, 204)), 0.5);
            pen.Freeze();
            brush.Freeze();

            int index = 0;
            while ((index = text.IndexOf(WordToHighlight, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                TextSegment segment = new TextSegment { StartOffset = index, Length = WordToHighlight.Length };

                BackgroundGeometryBuilder builder = new BackgroundGeometryBuilder();
                builder.AlignToWholePixels = true;
                builder.CornerRadius = 2;
                builder.AddSegment(textView, segment);

                Geometry geometry = builder.CreateGeometry();

                if (geometry != null)
                {
                    drawingContext.DrawGeometry(brush, pen, geometry);
                }

                index += WordToHighlight.Length;
            }
        }
    }
}
