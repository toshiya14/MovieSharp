using MovieSharp.Composers.Videos;
using MovieSharp.Objects;
using SkiaSharp;

namespace MovieSharp.Composers;

public interface IVideoClip : IDisposable
{
    /// <summary>
    /// The real size for this clip.
    /// </summary>
    Coordinate Size { get; }

    /// <summary>
    /// The real duration for this clip.
    /// </summary>
    double Duration { get; }

    /// <summary>
    /// Draw the frame for the specified time in canvas.
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="paint"></param>
    /// <param name="time"></param>
    void Draw(SKCanvas canvas, SKPaint? paint, double time);

    /// <summary>
    /// Release memories, but not disposed.
    /// </summary>
    void Release();
}

public static class IVideoClipExtensions
{
    public static IVideoClip MakeClipEx(this IVideoSource source, double maxCacheTime = 0.5)
    {
        return new VideoSourceCachedClip(source, maxCacheTime);
    }

    public static IVideoClip MakeClip(this IVideoSource source)
    {
        return new VideoSourceClip(source);
    }

    public static IVideoClip Crop(this IVideoClip clip, RectBound croparea)
    {
        return new CroppedVideoClipProxy(clip, croparea);
    }

    public static IVideoClip FollowedBy(this IVideoClip clip, IVideoClip other)
    {
        return new ConcatenatedVideoClipProxy(clip, other);
    }

    public static ITransformedVideoClip Transform(this IVideoClip clip)
    {
        if (clip is TransformedVideoClipProxy trans)
        {
            return trans;
        }
        else
        {
            return new TransformedVideoClipProxy(clip);
        }
    }

    public static IVideoClip Slice(this IVideoClip clip, double start, double end)
    {
        return new SlicedVideoClipProxy(clip, start, end);
    }

    public static IFilteredVideoClip Filter(this IVideoClip clip)
    {
        if (clip is FilteredVideoClipProxy p)
        {
            return p;
        }
        else
        {
            return new FilteredVideoClipProxy(clip);
        }
    }

    public static IVideoClip ChangeSpeed(this IVideoClip clip, double speed)
    {
        return new SpeedChangedVideoClipProxy(clip, speed);
    }

    public static IVideoClip RepeatTo(this IVideoClip clip, double newDuration)
    {
        return new RepeatedVideoClipProxy(clip, newDuration);
    }

    public static IVideoClip RepeatTimes(this IVideoClip clip, int times)
    {
        return new RepeatedVideoClipProxy(clip, clip.Duration * times);
    }

    public static IVideoClip Concatenate(this IEnumerable<IVideoClip> videoClips)
    {
        IVideoClip clip = new ZeroVideoClip(0, 0);
        if (videoClips == null || !videoClips.Any())
        {
            return clip;
        }
        foreach (var v in videoClips)
        {
            clip = clip.FollowedBy(v);
        }
        return clip;
    }
}
