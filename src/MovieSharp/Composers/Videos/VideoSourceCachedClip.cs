using System.Collections.Specialized;
using MovieSharp;
using MovieSharp.Composers;
using MovieSharp.Composers.Videos;
using MovieSharp.Debugs.Benchmarks;
using MovieSharp.Exceptions;
using MovieSharp.Objects;
using MovieSharp.Skia;
using MovieSharp.Sources.Videos;
using NLog;
using SkiaSharp;


internal class VideoSourceCachedClip : IVideoClip
{
    private readonly ILogger log = LogManager.GetCurrentClassLogger();

    private FFVideoFileSource FrameProvider { get; set; }

    private readonly int maxCacheFrames;
    private readonly double baseclipFramesGap;

    public Coordinate Size => this.FrameProvider.Size;
    public double Duration => this.FrameProvider.Duration;

    public PreloadFramePool FramePool { get; }

    /// <summary>
    /// Create VideoClip from IVideoSource, use `0` as start, and IVideoSource.Duration as end.
    /// </summary>
    /// <param name="source">The video source.</param>
    public VideoSourceCachedClip(IVideoSource source, int maxCacheFrames = 16)
    {
        this.maxCacheFrames = maxCacheFrames;
        if (source is FFVideoFileSource vidsrc)
        {
            this.FramePool = new PreloadFramePool(vidsrc.BytesPerFrame, vidsrc, maxCacheFrames);
            this.FrameProvider = vidsrc;
            this.baseclipFramesGap = 1.0 / vidsrc.FrameRate;
        }
        else
        {
            throw new Exception($"Only FFVideoFileSource supported cached proxy, but got {source.GetType().Name}.");
        }
    }

    /// <summary>
    /// Render the specified frame into the canvas.
    /// </summary>
    /// <param name="canvas">the skia canvas.</param>
    /// <param name="offsetTime">The offset time from the start of this clip.</param>
    public unsafe void Draw(SKCanvas canvas, SKPaint? paint, double offsetTime)
    {
        if (offsetTime > this.Duration || offsetTime < 0)
        {
            this.log.Warn($"Should not draw {offsetTime}, because it is not in the duration: {this.Duration}");
            return;
        }

        var findex = this.FrameProvider.GetFrameId(offsetTime);

        this.FramePool.Prepare(findex, this.maxCacheFrames / 2);

        var data = this.FramePool.WaitFrame(findex);
        using var handle = data.Pin();

        var img = SKImage.FromPixels(this.FrameProvider.ImageInfo, (nint)handle.Pointer);
        canvas.DrawImage(img, new SKPoint(0, 0), paint);

        this.FramePool.Free(findex);
    }

    public void Release()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        this.FramePool.Dispose();
        this.FrameProvider.Dispose();
    }
}

