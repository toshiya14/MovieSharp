using MovieSharp;
using MovieSharp.Composers;
using MovieSharp.Objects;
using MovieSharp.Skia;
using NLog;
using SkiaSharp;
internal class VideoSourceClip : IVideoClip
{
    private IVideoSource FrameProvider { get; set; }
    public Coordinate Size => this.FrameProvider.Size;
    public double Duration => this.FrameProvider.Duration;
    private readonly ILogger log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Create VideoClip from IVideoSource, use `0` as start, and IVideoSource.Duration as end.
    /// </summary>
    /// <param name="source">The video source.</param>
    public VideoSourceClip(IVideoSource source)
    {
        this.FrameProvider = source;
    }

    /// <summary>
    /// Get frame from the video source, the time would be mapped.
    /// </summary>
    /// <param name="offsetTime">The offset time from the start of this clip.</param>
    public Memory<byte>? GetFrame(double offsetTime)
    {
        if (offsetTime > this.Duration || offsetTime < 0)
        {
            return null;
        }

        return this.FrameProvider.MakeFrameByTime(offsetTime);
    }

    /// <summary>
    /// Render the specified frame into the canvas.
    /// </summary>
    /// <param name="canvas">the skia canvas.</param>
    /// <param name="offsetTime">The offset time from the start of this clip.</param>
    public void Draw(SKCanvas canvas, SKPaint? paint, double offsetTime)
    {
        this.log.Debug($"Draw on {offsetTime}");
        if (offsetTime > this.Duration || offsetTime < 0)
        {
            return;
        }
        canvas.DrawVideoFrame(paint, this.FrameProvider, offsetTime);
    }

    public void Dispose()
    {
        this.FrameProvider.Dispose();
        GC.SuppressFinalize(this);
    }
}

