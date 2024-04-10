using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MovieSharp.Objects.Subtitles.Drawings;
using MovieSharp.Objects.Subtitles;
using MovieSharp.Skia.Surfaces;
using NLog;
using MovieSharp.Objects;
using MovieSharp.Tools;
using SkiaSharp;
using NAudio.CoreAudioApi;
using MovieSharp.Debugs.Benchmarks;

namespace MovieSharp.Sources.Subtitles;

internal static class SKBoundExtensions
{
    public static SKRect ToSKRect(this (float x, float y, float w, float h) bound)
    {
        return new SKRect(bound.x, bound.y, bound.x + bound.w, bound.y + bound.h);
    }
}

internal abstract class SubtitleSourceBase : IVideoSource, ISubtitleSource
{
    private readonly ILogger log = LogManager.GetLogger(nameof(SubtitleSourceBase));

    protected List<TimelineItem> TimelineItems { get; private set; } = new List<TimelineItem>();

    public double FrameRate { get; }

    public Coordinate Size { get; }

    public PixelFormat PixelFormat { get; }

    public SKImageInfo ImageInfo { get; }

    protected ISurfaceProxy Surface { get; }

    protected DrawingTextBox? LastTextBox { get; set; }

    public int FrameCount => (int)(this.Duration * this.FrameRate);

    public virtual double Duration
    {
        get
        {
            if (this.TimelineItems.Count == 0)
            {
                return 0;
            }
            else
            {
                return this.TimelineItems.Select(x => x.End).Max();
            }
        }
    }

    public SubtitleSourceBase((int width, int height) renderBound, double framerate)
    {
        this.Size = new Coordinate(renderBound);
        this.FrameRate = framerate;
        this.PixelFormat = PixelFormat.RGBA32; this.ImageInfo = new SKImageInfo(renderBound.width, renderBound.height, this.PixelFormat.GetColorType(), SKAlphaType.Unpremul);
        this.Surface = new RasterSurface(this.ImageInfo);
    }

    public virtual IVideoSource AsVideoSource()
    {
        return this;
    }

    public void From(SubtitleTimelineBuilder stb)
    {
        this.TimelineItems = stb.Make();
    }
    public void AppendTimeline(TimelineItem item)
    {
        this.TimelineItems.Add(item);
    }

    public virtual int GetFrameId(double time) => (int)(this.FrameRate * time + 0.000001);


    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public virtual SKBitmap? MakeFrameById(int frameIndex)
    {
        return this.MakeFrameByTime(frameIndex / this.FrameRate);
    }

    #region Measure Texts

    private int FindWrap(FontDefinition font, string text, float limit)
    {
        for (var i = 1; i < text.Length; i++)
        {
            var trueWidth = this.Measure(text[..i], font, out var bound);
            if (trueWidth > limit)
            {
                return i - 1;
            }
        }
        return text.Length - 1;
    }

    public virtual DrawingTextBox MeasureText(TimelineItem text)
    {
        var box = new DrawingTextBox();

        box.BoxSize.X = this.Size.X;
        var widthLimit = box.BoxSize.X;
        var widthRest = (float)widthLimit;
        var line = new DrawingTextLine(box, 1, 0f);
        var contentIndex = 0;

        var nextRun = text.Contents[0];

        while (true)
        {
            var trueWidth = this.Measure(nextRun.Text, nextRun.Font, out var bound);
            var shouldWrapLine = trueWidth > widthLimit || nextRun.Text.StartsWith('\n');

            if (!shouldWrapLine)
            {
                line.AddContent(nextRun, widthLimit - widthRest, bound.ToSKRect(), trueWidth);
                widthRest -= trueWidth;
                contentIndex++;

                // finish measurement.
                if (contentIndex >= text.Contents.Count)
                {
                    // add last line.
                    line.Left = text.TextAlign switch
                    {
                        TextAlign.Left => 0,
                        TextAlign.Center => widthRest / 2,
                        TextAlign.Right => widthRest,
                        _ => throw new NotSupportedException($"Not supported TextAlign: {text.TextAlign}.")
                    };
                    box.Lines.Add(line);
                    break;
                }

                nextRun = text.Contents[contentIndex];
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
                    var wrapPos = this.FindWrap(nextRun.Font, nextRun.Text, widthRest);
                    if (wrapPos < 1)
                    {
                        throw new ArgumentException("FontSize too large, even could not draw 1 character for each line.");
                    }
                    var (left, right) = nextRun.Fission(wrapPos);
                    var widthIncludesWhiteSpace = this.Measure(left.Text, nextRun.Font, out var lb);
                    line.AddContent(left, widthLimit - widthRest, lb.ToSKRect(), widthIncludesWhiteSpace);
                    nextRun = right;
                    widthRest -= widthIncludesWhiteSpace;
                }

                // new line.
                line.Left += text.TextAlign switch
                {
                    TextAlign.Left => 0,
                    TextAlign.Center => widthRest / 2,
                    TextAlign.Right => widthRest,
                    _ => throw new NotSupportedException($"Not supported TextAlign: {text.TextAlign}.")
                };
                box.Lines.Add(line);
                var top = line.Top + line.MaxHeight + text.LineSpacing;
                line = new DrawingTextLine(box, box.Lines.Count, top);
                widthRest = widthLimit;
            }
        }

        // set the property for textbox.
        box.BoxSize.Y = (int)Math.Ceiling(line.Top + line.MaxHeight);
        box.Position = new Coordinate(
        (int)(this.Size.X * text.Anchor.X - box.BoxSize.X * text.Anchor.X),
            (int)(this.Size.Y * text.Anchor.Y - box.BoxSize.Y * text.Anchor.Y)
        );
        return box;
    }

    public abstract float Measure(string text, FontDefinition font, out (float x, float y, float w, float h) bound);

    #endregion

    public virtual SKBitmap? MakeFrameByTime(double t)
    {
        using var _ = PerformanceMeasurer.UseMeasurer("make-subtitle");

        var cvs = this.Surface.Canvas;
        cvs.Clear();

        foreach (var text in this.TimelineItems.Where(x => x.Start <= t && x.End >= t))
        {
            if (text.Contents.Count == 0)
            {
                continue;
            }
            var box = this.MeasureText(text);
            this.DrawTextBox(box);
        }

        cvs.Flush();
        var img = this.Surface.Snapshot();
        return SKBitmap.FromImage(img).Copy();
    }

    protected abstract void DrawTextBox(DrawingTextBox text);
}
