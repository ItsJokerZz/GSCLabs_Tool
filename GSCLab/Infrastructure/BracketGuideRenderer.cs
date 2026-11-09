using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Document;
using System.Windows.Media;
using System.Windows;

namespace GSCLab.Infrastructure
{
    public class BracketGuideRenderer : IBackgroundRenderer
    {
        public int StartOffset { get; set; } = -1;
        public int EndOffset { get; set; } = -1;

        public KnownLayer Layer => KnownLayer.Background;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (StartOffset < 0 || EndOffset < 0) return;

            try
            {
                var builder = new BackgroundGeometryBuilder
                {
                    AlignToWholePixels = true,
                    CornerRadius = 2
                };

                var bgBrush = new SolidColorBrush(Color.FromArgb(100, 0, 122, 204));
                var borderPen = new Pen(new SolidColorBrush(Color.FromRgb(0, 122, 204)), 1);

                var linePen = new Pen(new SolidColorBrush(Color.FromArgb(150, 0, 122, 204)), 1)
                {
                    DashStyle = DashStyles.Dash
                };

                if (StartOffset >= textView.Document.TextLength || EndOffset >= textView.Document.TextLength) return;

                builder.AddSegment(textView, new TextSegment { StartOffset = StartOffset, Length = 1 });
                builder.AddSegment(textView, new TextSegment { StartOffset = EndOffset, Length = 1 });

                var geometry = builder.CreateGeometry();
                if (geometry != null)
                {
                    drawingContext.DrawGeometry(bgBrush, borderPen, geometry);
                }

                var startExtents = BackgroundGeometryBuilder.GetRectsForSegment(textView, new TextSegment { StartOffset = StartOffset, Length = 1 }).ToList();
                var endExtents = BackgroundGeometryBuilder.GetRectsForSegment(textView, new TextSegment { StartOffset = EndOffset, Length = 1 }).ToList();

                if (startExtents.Count > 0 && endExtents.Count > 0)
                {
                    Rect startRect = startExtents[0];
                    Rect endRect = endExtents[0];

                    if (startRect.Bottom < endRect.Top)
                    {
                        double xPos = startRect.Left + (startRect.Width / 2);
                        drawingContext.DrawLine(linePen, new Point(xPos, startRect.Bottom + 2), new Point(xPos, endRect.Top - 2));
                    }
                    else if (endRect.Bottom < startRect.Top)
                    {
                        double xPos = endRect.Left + (endRect.Width / 2);
                        drawingContext.DrawLine(linePen, new Point(xPos, endRect.Bottom + 2), new Point(xPos, startRect.Top - 2));
                    }
                }
            }
            catch
            {
            }
        }
    }
}
