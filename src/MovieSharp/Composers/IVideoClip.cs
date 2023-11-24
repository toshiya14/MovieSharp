using MovieSharp.Composers.Videos;
using MovieSharp.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp.Composers;

public interface IVideoClip : IDisposable
{
    Coordinate Size { get; }
    double Duration { get; }
    void Draw(SKCanvas canvas, double time);
}

public static class IVideoClipExtensions
{
    public static IVideoClip ToClip(this IVideoSource source)
    {
        return new VideoSourceClip(source);
    }

    public static IVideoClip Crop(this IVideoClip clip, RectBound croparea)
    {
        return new CroppedVideoClipProxy(clip, croparea);
    }

    public static ITransformableVideo AsTransformable(this IVideoClip clip)
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
}
