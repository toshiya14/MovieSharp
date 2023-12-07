using System.Collections.Specialized;
using MovieSharp;
using MovieSharp.Composers;
using MovieSharp.Debugs.Benchmarks;
using MovieSharp.Objects;
using MovieSharp.Skia;
using NLog;
using SkiaSharp;
internal class VideoSourceClip : IVideoClip
{
    private readonly ILogger log = LogManager.GetCurrentClassLogger();

    private IVideoSource FrameProvider { get; set; }

    private readonly double maxCacheTime;
    private readonly double baseclipFramesGap;

    public Coordinate Size => this.FrameProvider.Size;
    public double Duration => this.FrameProvider.Duration;

    private Dictionary<int, (bool loading, SKBitmap? frame)> FrameCache = new();
    private Task? LoadingTask { get; set; }

    /// <summary>
    /// Create VideoClip from IVideoSource, use `0` as start, and IVideoSource.Duration as end.
    /// </summary>
    /// <param name="source">The video source.</param>
    public VideoSourceClip(IVideoSource source, double maxCacheTime = 0.5)
    {
        this.FrameProvider = source;
        this.maxCacheTime = maxCacheTime;
        this.baseclipFramesGap = 1.0 / this.FrameProvider.FrameRate;
        //this.log.Trace($"frame gap: {this.baseclipFramesGap}");
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

        var findex = this.FrameProvider.GetFrameId(offsetTime);

        if (!this.FrameCache.ContainsKey(findex))
        {
            var end = this.FrameProvider.GetFrameId(Math.Min(this.FrameProvider.Duration, offsetTime + this.maxCacheTime));
            this.ReloadCache(findex, end - findex);
        }

        if (!this.FrameCache.ContainsKey(findex))
        {
            return;
        }

        while (this.FrameCache[findex].loading)
        {
            Thread.Sleep(1);
        }

        using var _ = PerformanceMeasurer.UseMeasurer("videosrc-drawing");
        var bmp = this.BestMatchCache(findex);
        if (bmp is not null)
        {
            canvas.DrawBitmap(bmp, 0, 0, paint);
        }
        else
        {
            this.log.Warn($"Get null bitmap from video source @ {offsetTime}.");
        }
    }

    private void ReloadCache(int findex, int count)
    {
        this.LoadingTask?.Wait();

        // reload
        foreach (var (_, entry) in this.FrameCache)
        {
            entry.frame?.Dispose();
        }
        this.FrameCache.Clear();
        for (var i = findex; i < findex + count; i++)
        {
            this.FrameCache[i] = (true, null);
        }

        this.LoadingTask = Task.Run(() =>
        {
            using var _ = PerformanceMeasurer.UseMeasurer($"reload-cache-{count}");
            for (var i = findex; i <= findex + count; i++)
            {
                var pixels = this.FrameProvider.MakeFrameById(i);
                this.FrameCache[i] = (false, pixels);
            }
            //this.log.Trace($"Preload frames: {this.maxCacheTime}s {this.FrameCache.Count} frames");
        });
    }

    private SKBitmap? BestMatchCache(int findex)
    {
        using var _ = PerformanceMeasurer.UseMeasurer("cache-match");
        if (this.FrameCache.ContainsKey(findex))
        {
            while (this.FrameCache[findex].loading)
            {
                Thread.Sleep(1);
            }

            var (_, bmp) = this.FrameCache[findex];
            return bmp;
        }
        return null;
    }

    public void Dispose()
    {
        foreach (var entry in this.FrameCache.Values)
        {
            var (loading, frame) = entry;
            frame?.Dispose();
        }
        this.FrameProvider.Dispose();
        GC.SuppressFinalize(this);
    }
}

