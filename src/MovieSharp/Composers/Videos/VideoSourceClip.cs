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
    private readonly Memory<byte>[] cache = new Memory<byte>[MovieSharpModuleConfig.LoaderCacheFrames];
    private readonly int?[] cacheFrameIndexMap = new int?[MovieSharpModuleConfig.LoaderCacheFrames];
    private readonly Queue<int> cacheIndexQueue = new Queue<int>();
    private bool CanDraw(double time)
    {
        var findex = this.FrameProvider.GetFrameId(time);
        var inqueue = this.cacheIndexQueue.Contains(findex);
        var cacheIndex = this.GetCacheId(findex);
        return inqueue || cacheIndex >= 0;
    }

    /// <summary>
    /// Create VideoClip from IVideoSource, use `0` as start, and IVideoSource.Duration as end.
    /// </summary>
    /// <param name="source">The video source.</param>
    public VideoSourceClip(IVideoSource source)
    {
        this.FrameProvider = source;
        var frameSize = source.Size.X * source.Size.Y * 4;
        for (var i = 0; i < MovieSharpModuleConfig.LoaderCacheFrames; i++)
        {
            this.cache[i] = new Memory<byte>(new byte[frameSize]);
        }
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
            this.log.Warn($"Won't draw {offsetTime}, because it is not in the duration: {this.Duration}");
            return;
        }

        var findex = this.FrameProvider.GetFrameId(offsetTime);
        var frame = this.WaitFrame(findex);

        using var _ = PerformanceMeasurer.UseMeasurer("videosrc-drawing");
        using var bmp = new SKBitmap();
        bmp.InstallPixels()

        if (bmp is not null)
        {
            canvas.DrawBitmap(bmp, 0, 0, paint);
        }
        else
        {
            //this.log.Warn($"Get null bitmap from video source @ {offsetTime}.");
        }
    }

    //private SKBitmap? WaitFrame(int findex)
    //{
    //    while (this.FrameCache[findex].loading)
    //    {
    //        if (this.LoadingTask == null)
    //        {
    //            return null;
    //        }
    //        if (this.LoadingTask.IsCompleted)
    //        {
    //            if (this.LoadingTask.Exception is not null)
    //            {
    //                throw this.LoadingTask.Exception;
    //            }
    //        }
    //    }

    //    return this.FrameCache[findex].frame;
    //}

    //private void ReloadCache(int findex, int count)
    //{
    //    this.LoadingTask?.Wait();

    //    // reload
    //    var list = new List<int>();
    //    foreach (var (i, entry) in this.FrameCache)
    //    {
    //        list.Add(i);
    //        entry.frame?.Dispose();
    //    }
    //    //if (MovieSharpModuleConfig.RunInTestMode) this.log.Trace($"Clear frame cache, Dispposed: {string.Join(',', list)}");
    //    this.FrameCache.Clear();
    //    for (var i = findex; i <= findex + count; i++)
    //    {
    //        this.FrameCache[i] = (true, null);
    //    }

    //    //if (MovieSharpModuleConfig.RunInTestMode) this.log.Trace($"Queued loading: {string.Join(',', this.FrameCache.Keys)}");

    //    this.LoadingTask = Task.Run(() =>
    //    {
    //        using var _ = PerformanceMeasurer.UseMeasurer($"reload-cache-{count}");
    //        for (var i = findex; i <= findex + count; i++)
    //        {
    //            var pixels = this.FrameProvider.MakeFrameById(i);
    //            this.FrameCache[i] = (false, pixels);
    //        }
    //        //this.log.Trace($"Preload frames: {this.maxCacheTime}s {this.FrameCache.Count} frames");
    //    });
    //}

    public void Preload(int findex)
    {
        while (this.cacheIndexQueue.Count > MovieSharpModuleConfig.LoaderCacheFrames)
        {
            Thread.Sleep(10);
        }

        this.cacheIndexQueue.Enqueue(findex);
    }

    public Memory<byte> WaitFrame(int findex)
    {
        while (true)
        {
            var cacheId = this.GetCacheId(findex);
            if (cacheId != -1)
            {
                return this.cache[cacheId];
            }

            if (this.CheckIfCacheIsFull())
            {
                throw new Exception($"frame {findex} could not be wait, because it is not in cache.");
            }

            Thread.Sleep(10);
        }
    }

    private void LoadSingleFrameInBackground()
    {
        if (this.cacheIndexQueue.TryDequeue(out var findex))
        {
            var i = this.FindEmptyCacheSlot();
            this.cacheFrameIndexMap[i] = findex;
            var mem = this.FrameProvider.MakeFrame(findex);
        }
    }

    private int FindEmptyCacheSlot()
    {
        while (true)
        {
            for (var i = 0; i < this.cacheFrameIndexMap.Length; i++)
            {
                var slot = this.cacheFrameIndexMap[i];
                if (slot is null)
                {
                    return i;
                }
            }

            Thread.Sleep(10);
        }
    }

    private bool CheckIfCacheIsFull()
    {
        for (var i = 0; i < this.cacheFrameIndexMap.Length; i++)
        {
            var slot = this.cacheFrameIndexMap[i];
            if (slot is null)
            {
                return false;
            }
        }

        return true;
    }

    private int GetCacheId(int findex)
    {
        for (var i = 0; i < this.cacheFrameIndexMap.Length; i++)
        {
            if (findex == this.cacheFrameIndexMap[i])
            {
                return i;
            }
        }

        return -1;
    }

    public void Dispose()
    {
        //foreach (var entry in this.FrameCache.Values)
        //{
        //    var (loading, frame) = entry;
        //    frame?.Dispose();
        //}
        this.FrameProvider.Dispose();
        GC.SuppressFinalize(this);
    }
}

