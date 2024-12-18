using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MovieSharp.Sources.Videos;
using NLog;

namespace MovieSharp.Composers.Videos;
internal class PreloadFramePool : IDisposable
{
    private enum QueueType
    {
        Predicated,
        Specified
    }
    private record FrameCacheMetaData(int FrameIndex, bool FinishedLoading, bool ShouldDispose, int MemoryIndex, QueueType QueueType, Task LoadingTask);

    //private readonly Dictionary<int, int> FrameCacheIndexMapper = new();
    //private readonly Dictionary<int, bool> FrameCacheFinishLoadState = new();
    //private readonly Dictionary<int, bool> FrameCacheDisposeFlag = new();
    private readonly ConcurrentDictionary<int, FrameCacheMetaData> FrameCache = new();
    private readonly CancellationTokenSource cts;
    private readonly Logger log = LogManager.GetLogger("PreloadFramePool");
    private const int MaxWaitTime = 10000;

    public PreloadFramePool(long fragmentSize, FFVideoFileSource handleSource, int poolSize = 16)
    {
        this.PoolSize = poolSize;
        this.HandleSource = handleSource;
        this.MemoryPool = AllocateMemory(poolSize, fragmentSize);
        this.cts = new CancellationTokenSource();
        this.HandleSource.OnReInitialized += this.SourceReinitialized;
    }

    private void SourceReinitialized(object? sender, FFVideoReInitializedEventArgs e)
    {
        this.FrameCache.Clear();
    }

    public int PoolSize { get; }
    public Memory<byte>[] MemoryPool { get; }
    public int LoadingPosition { get; private set; }
    public FFVideoFileSource HandleSource { get; }

    private static Memory<byte>[] AllocateMemory(int count, long fragmentSize)
    {
        var list = new List<Memory<byte>>();
        for (var i = 0; i < count; i++)
        {
            var fragment = new byte[fragmentSize];
            list.Add(fragment.AsMemory());
        }
        return list.ToArray();
    }

    private (int index, Memory<byte> memory)? GetFreeMemory()
    {
        var indexes = new bool[this.PoolSize].Select((x, i) => i).ToList();
        foreach (var meta in this.FrameCache.Values)
        {
            if (!meta.ShouldDispose)
            {
                indexes.Remove(meta.MemoryIndex);
            }
        }
        if (indexes.Any())
        {
            var index = indexes.First();
            return (index, this.MemoryPool[index]);
        }
        return null;
    }

    private void Clean()
    {
        var tobeRemoved = new List<int>();
        foreach (var (findex, frame) in this.FrameCache)
        {
            if (frame.ShouldDispose)
            {
                tobeRemoved.Add(findex);
            }
        }

        foreach (var findex in tobeRemoved)
        {
            this.FrameCache.Remove(findex, out _);
        }

        if (tobeRemoved.Count > 0)
        {
            this.log.Debug($"Clean() called, removed: {string.Join(',', tobeRemoved)}");
        }
    }

    public void Prepare(int findex, int count)
    {
        for (var i = findex; i < findex + count; i++)
        {
            if (this.FrameCache.TryGetValue(findex, out var cache))
            {
                if (cache.QueueType == QueueType.Predicated)
                {
                    this.FrameCache[findex] = cache with { QueueType = QueueType.Specified };
                }
                else
                {
                }
            }
        }
    }

    public ReadOnlyMemory<byte> WaitFrame(int findex)
    {
        var sw = Stopwatch.StartNew();
        while (true)
        {
            if (this.FrameCache.TryGetValue(findex, out var frame))
            {
                if (frame.FinishedLoading)
                {
                    var mindex = frame.MemoryIndex;
                    return this.MemoryPool[mindex];
                }
            }

            if (sw.ElapsedMilliseconds >= MaxWaitTime)
            {
                sw.Stop();
                throw new Exception($"WaitFrame({findex}) over time, did you remembered to call Prepare() for them?");
            }

            Task.Delay(5).Wait();
        }
    }

    public void Free(int findex)
    {
        if (this.FrameCache.TryGetValue(findex, out var value))
        {
            this.FrameCache[findex] = value with { ShouldDispose = true };
        }
    }

    public void Dispose()
    {
        this.cts.Cancel();
        for (var i = 0; i < this.MemoryPool.Length; i++)
        {
            this.MemoryPool[i] = new Memory<byte>();
        }
        GC.Collect();
        GC.SuppressFinalize(this);
    }
}
