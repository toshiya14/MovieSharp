using MovieSharp.Composers.Videos;
using MovieSharp.Objects;
using SkiaSharp;

namespace MovieSharp.Composers;

public interface IVideoClip : IDisposable
{
    Coordinate Size { get; }
    double Duration { get; }
    void Draw(SKCanvas canvas, SKPaint? paint, double time);
}

public static class IVideoClipExtensions
{
    public static IVideoClip MakeClip(this IVideoSource source)
    {
        return new VideoSourceClip(source);
    }

    public static IVideoClip Crop(this IVideoClip clip, RectBound croparea)
    {
        return new CroppedVideoClipProxy(clip, croparea);
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
}
