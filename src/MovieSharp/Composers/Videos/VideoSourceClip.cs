using MovieSharp;
using MovieSharp.Composers;
using MovieSharp.Debugs.Benchmarks;
using MovieSharp.Objects;
using MovieSharp.Skia;
using NLog;
using SkiaSharp;
internal class VideoSourceClip : IVideoClip
{
    private IVideoSource FrameProvider { get; set; }
    public Coordinate Size => this.FrameProvider.Size;
    public double Duration => this.FrameProvider.Duration;
    //private readonly ILogger log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Create VideoClip from IVideoSource, use `0` as start, and IVideoSource.Duration as end.
    /// </summary>
    /// <param name="source">The video source.</param>
    public VideoSourceClip(IVideoSource source)
    {
        this.FrameProvider = source;
    }

    /// <summary>
    /// Render the specified frame into the canvas.
    /// </summary>
    /// <param name="canvas">the skia canvas.</param>
    /// <param name="offsetTime">The offset time from the start of this clip.</param>
    public void Draw(SKCanvas canvas, SKPaint? paint, double offsetTime)
    {
        //this.log.Debug($"Draw on {offsetTime}");
        if (offsetTime > this.Duration || offsetTime < 0)
        {
            return;
        }

        var pm = PerformanceMeasurer.GetCurrentClassMeasurer();
        using var _ = pm.UseMeasurer("videosrc-drawing");
        using var bmp = this.FrameProvider.MakeFrameByTime(offsetTime);

        canvas.DrawBitmap(bmp, 0, 0, paint);
    }

    public void Dispose()
    {
        this.FrameProvider.Dispose();
        GC.SuppressFinalize(this);
    }
}

