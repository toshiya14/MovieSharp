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
        if (offsetTime > this.Duration || offsetTime < 0)
        {
            this.log.Warn($"Should not draw {offsetTime}, because it is not in the duration: {this.Duration}");
            return;
        }

        var findex = this.FrameProvider.GetFrameId(offsetTime);
        //if (MovieSharpModuleConfig.RunInTestMode) this.log.Trace($"Draw on {findex}, converted from offset time: {offsetTime}.");

        if (!this.FrameCache.ContainsKey(findex))
        {
            //if (MovieSharpModuleConfig.RunInTestMode) this.log.Trace($"Cache not matched for {findex}, reload.");
            var end = this.FrameProvider.GetFrameId(Math.Min(this.FrameProvider.Duration, offsetTime + this.maxCacheTime));
            this.ReloadCache(findex, end - findex);
        }

        if (!this.FrameCache.ContainsKey(findex))
        {
            return;
        }

        var bmp = this.WaitFrame(findex);

        using var _ = PerformanceMeasurer.UseMeasurer("videosrc-drawing");

        if (bmp is not null)
        {
            canvas.DrawBitmap(bmp, 0, 0, paint);
        }
        else
        {
            //this.log.Warn($"Get null bitmap from video source @ {offsetTime}.");
        }
    }

    private SKBitmap? WaitFrame(int findex)
    {
        while (this.FrameCache[findex].loading)
        {
            if (this.LoadingTask == null)
            {
                return null;
            }
            if (this.LoadingTask.IsCompleted)
            {
                if (this.LoadingTask.Exception is not null)
                {
                    throw this.LoadingTask.Exception;
                }
            }
        }

        return this.FrameCache[findex].frame;
    }

    private void ReloadCache(int findex, int count)
    {
        this.LoadingTask?.Wait();

        // reload
        var list = new List<int>();
        foreach (var (i, entry) in this.FrameCache)
        {
            list.Add(i);
            entry.frame?.Dispose();
        }
        //if (MovieSharpModuleConfig.RunInTestMode) this.log.Trace($"Clear frame cache, Dispposed: {string.Join(',', list)}");
        this.FrameCache.Clear();
        for (var i = findex; i <= findex + count; i++)
        {
            this.FrameCache[i] = (true, null);
        }

        //if (MovieSharpModuleConfig.RunInTestMode) this.log.Trace($"Queued loading: {string.Join(',', this.FrameCache.Keys)}");

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

