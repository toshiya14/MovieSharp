using System.Collections.Specialized;
using MovieSharp;
using MovieSharp.Composers;
using MovieSharp.Debugs.Benchmarks;
using MovieSharp.Exceptions;
using MovieSharp.Objects;
using MovieSharp.Skia;
using NLog;
using SkiaSharp;
internal class VideoSourceClip : IVideoClip
{
    private readonly ILogger log = LogManager.GetCurrentClassLogger();

    private IVideoSource FrameProvider { get; set; }

    public Coordinate Size => this.FrameProvider.Size;
    public double Duration => this.FrameProvider.Duration;

    private SKBitmap? frame;

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
        if (this.frame == null)
        {
            this.frame = new SKBitmap(new SKImageInfo(this.Size.X, this.Size.Y, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        }

        if (offsetTime > this.Duration || offsetTime < 0)
        {
            this.log.Debug($"Should not draw {offsetTime}, because it is not in the duration: {this.Duration}");
            return;
        }

        var findex = this.FrameProvider.GetFrameId(offsetTime);

        this.WaitFrame(this.frame, findex);

        using var _ = PerformanceMeasurer.UseMeasurer("videosrc-drawing");
        canvas.DrawBitmap(this.frame, 0, 0, paint);
    }

    private void WaitFrame(SKBitmap frame, int findex)
    {
        using var _ = PerformanceMeasurer.UseMeasurer($"reload-frame");
        this.FrameProvider.MakeFrameById(frame, findex);
    }

    public void Release()
    {
        this.frame?.Dispose();
        this.FrameProvider?.Close(true);
    }

    public void Dispose()
    {
        this.frame?.Dispose();
        this.FrameProvider.Dispose();
        GC.SuppressFinalize(this);
    }
}

