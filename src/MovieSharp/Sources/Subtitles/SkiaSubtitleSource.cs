using IronSoftware.Drawing;
using MovieSharp.Debugs.Benchmarks;
using MovieSharp.Fonts;
using MovieSharp.Objects;
using MovieSharp.Objects.Subtitles;
using MovieSharp.Objects.Subtitles.Drawings;
using MovieSharp.Skia.Surfaces;
using MovieSharp.Tools;
using NLog;
using SkiaSharp;

namespace MovieSharp.Sources.Subtitles;

internal class SkiaSubtitleSource : SubtitleSourceBase
{
    private readonly ILogger log = LogManager.GetLogger(nameof(SkiaSubtitleSource));

    public IFontManager<SKFont> FontManager { get; }

    public SkiaSubtitleSource((int, int) renderBound, double framerate, IFontManager<SKFont>? cache = null) : base(renderBound, framerate)
    {
        this.FontManager = cache ?? new SkiaFontManager();
    }

    public override float Measure(string text, FontDefinition font, out (float x, float y, float w, float h) bound)
    {
        using var paint = font.CreateMeasurePaint(this.FontManager);
        var skbound = new SKRect();
        var trueWidth = paint.MeasureText(text, ref skbound);
        bound = (skbound.Left, skbound.Top, skbound.Width, skbound.Height);
        return trueWidth;
    }

    protected override void DrawTextBox(SKCanvas cvs, DrawingTextBox text, (int x, int y) originPosition)
    {
        foreach (var line in text.Lines)
        {
            foreach (var (run, pos) in line.Enumerate())
            {
                var (x, y) = pos;
                x += originPosition.x;
                y += originPosition.y;

                var stroke = run.Font!.CreateStrokePaint(this.FontManager);
                var fill = run.Font.CreateFillPaint(this.FontManager);
                var position = new SKPoint(x + run.LeadingSpace, y + run.Ascent);
                // Draw Stroke
                cvs.DrawText(run.Text, position, stroke);
                // Draw Fill
                cvs.DrawText(run.Text, position, fill);
            }
        }
    }

    public override void Dispose()
    {
        base.Dispose();
    }
}
