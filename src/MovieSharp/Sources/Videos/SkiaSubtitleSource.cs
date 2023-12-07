using MovieSharp.Debugs.Benchmarks;
using MovieSharp.Objects;
using MovieSharp.Objects.Subtitles;
using MovieSharp.Objects.Subtitles.Drawings;
using MovieSharp.Skia.Surfaces;
using MovieSharp.Tools;
using NLog;
using SkiaSharp;

namespace MovieSharp.Sources.Videos;

internal class SkiaSubtitleSource : IVideoSource, ISubtitleSource
{
    private readonly ILogger log = LogManager.GetLogger(nameof(SkiaSubtitleSource));

    private List<TimelineItem> items;
    private DrawingTextBox? lastTextBox;
    private ISurfaceProxy surface;

    public int FrameCount => (int)(this.Duration * this.FrameRate);

    public double FrameRate { get; }

    public Coordinate Size { get; }

    public PixelFormat PixelFormat { get; }
    public double Duration => this.items.Select(x => x.End).Max();
    public SKImageInfo ImageInfo { get; }

    public FontCache FontCache { get; }

    public SkiaSubtitleSource((int, int) renderBound, double framerate, PixelFormat? pixfmt = null, FontCache? cache = null)
    {
        this.items = new List<TimelineItem>();
        this.Size = new Coordinate(renderBound);
        this.FrameRate = framerate;
        this.PixelFormat = pixfmt ?? PixelFormat.RGBA32;

        var (w, h) = this.Size;
        this.ImageInfo = new SKImageInfo(w, h, this.PixelFormat.GetColorType(), SKAlphaType.Unpremul);
        this.surface = new RasterSurface(this.ImageInfo);
        this.FontCache = cache ?? new FontCache();
    }

    public void From(SubtitleTimelineBuilder stb)
    {
        this.items = stb.Make();
    }

    public IVideoSource AsVideoSource()
    {
        return this;
    }

    public SKBitmap? MakeFrameById(int frameIndex)
    {
        //this.log.Trace($"MakeFrameById: {frameIndex} / {this.FrameCount}");
        return this.MakeFrameByTime(frameIndex / this.FrameRate);
    }

    public int GetFrameId(double time) => (int)(this.FrameRate * time + 0.000001);

    public SKBitmap? MakeFrameByTime(double t)
    {
        //this.log.Trace($"MakeFrameByTime: {t} / {this.Duration}");
        using var _ = PerformanceMeasurer.UseMeasurer("make-subtitle");

        var cvs = this.surface.Canvas;
        cvs.Clear();

        foreach (var part in this.items.Where(x => x.Start <= t && x.End >= t))
        {
            this.DrawPart(cvs, part);
        }

        cvs.Flush();
        var img = this.surface.Snapshot();
        return SKBitmap.FromImage(img).Copy();
    }

    private static int FindWrap(SKPaint measurePaint, string text, float limit)
    {
        var bound = new SKRect();
        for (var i = 1; i < text.Length; i++)
        {
            var trueWidth = measurePaint.MeasureText(text[..i], ref bound);
            if (trueWidth > limit)
            {
                return i - 1;
            }
        }
        return text.Length - 1;
    }

    private DrawingTextBox MeasurePart(TimelineItem part)
    {
        var box = new DrawingTextBox();

        box.BoxSize.X = this.Size.X;
        var widthLimit = box.BoxSize.X;
        var widthRest = (float)widthLimit;
        var line = new DrawingTextLine(box, 1, 0f);
        var contentIndex = 0;

        var nextRun = part.Contents[0];
        var measurePaint = nextRun.Font.CreateMeasurePaint(this.FontCache);
        var bound = new SKRect();

        while (true)
        {
            var trueWidth = measurePaint.MeasureText(nextRun.Text, ref bound);
            var shouldWrapLine = trueWidth > widthLimit || nextRun.Text.StartsWith('\n');

            if (!shouldWrapLine)
            {
                line.AddContent(nextRun, widthLimit - widthRest, bound, trueWidth);
                widthRest -= trueWidth;
                contentIndex++;

                // finish measurement.
                if (contentIndex >= part.Contents.Count)
                {
                    // add last line.
                    line.Left = part.TextAlign switch
                    {
                        TextAlign.Left => 0,
                        TextAlign.Center => widthRest / 2,
                        TextAlign.Right => widthRest,
                        _ => throw new NotSupportedException($"Not supported TextAlign: {part.TextAlign}.")
                    };
                    box.Lines.Add(line);
                    break;
                }

                nextRun = part.Contents[contentIndex];
            }
            else
            {
                // find wrap position.
                if (nextRun.Text.StartsWith('\n'))
                {
                    // actions before add empty line.
                    nextRun = nextRun with { Text = nextRun.Text[1..] };
                }
                else
                {
                    // actions before add non-empty line.
                    var wrapPos = FindWrap(measurePaint, nextRun.Text, widthRest);
                    if (wrapPos < 1)
                    {
                        throw new ArgumentException("FontSize too large, even could not draw 1 character for each line.");
                    }
                    var (left, right) = nextRun.Fission(wrapPos);
                    var widthIncludesWhiteSpace = measurePaint.MeasureText(left.Text, ref bound);
                    line.AddContent(left, widthLimit - widthRest, bound, widthIncludesWhiteSpace);
                    nextRun = right;
                    widthRest -= widthIncludesWhiteSpace;
                }

                // new line.
                line.Left += part.TextAlign switch
                {
                    TextAlign.Left => 0,
                    TextAlign.Center => widthRest / 2,
                    TextAlign.Right => widthRest,
                    _ => throw new NotSupportedException($"Not supported TextAlign: {part.TextAlign}.")
                };
                box.Lines.Add(line);
                var top = line.Top + line.MaxHeight + part.LineSpacing;
                line = new DrawingTextLine(box, box.Lines.Count, top);
                widthRest = widthLimit;
            }
        }

        // set the property for textbox.
        box.BoxSize.Y = (int)Math.Ceiling(line.Top + line.MaxHeight);
        box.Position = new Coordinate(
            (int)(this.Size.X * part.Anchor.X - box.BoxSize.X * part.Anchor.X),
            (int)(this.Size.Y * part.Anchor.Y - box.BoxSize.Y * part.Anchor.Y)
        );
        this.lastTextBox = box;
        return box;
    }

    private void DrawPart(SKCanvas cvs, TimelineItem part)
    {
        if (part.Contents.Count == 0)
        {
            return;
        }

        var textbox = this.MeasurePart(part);
        foreach (var line in textbox.Lines)
        {
            foreach (var (run, pos) in line.Enumerate())
            {
                var (x, y) = pos;
                var stroke = run.Font.CreateStrokePaint(this.FontCache);
                var fill = run.Font.CreateFillPaint(this.FontCache);
                var position = new SKPoint(x + run.LeadingSpace, y + run.Ascent);
                // Draw Stroke
                cvs.DrawText(run.Text, position, stroke);
                // Draw Fill
                cvs.DrawText(run.Text, position, fill);
            }
        }
    }

    public void AppendSubtitle(TimelineItem item)
    {
        this.items.Add(item);
    }

    public void Dispose()
    {
        this.surface.Dispose();
        GC.SuppressFinalize(this);
    }

    #region TESTS
#if DEBUG
    private static bool IsNUnitRunning()
    {
        return AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.FullName?.ToLowerInvariant().StartsWith("nunit.framework") == true);
    }

    public DrawingTextBox? GetLastTextBox()
    {
        if (IsNUnitRunning())
        {
            return this.lastTextBox;
        }
        return null;
    }
#endif
    #endregion
}
