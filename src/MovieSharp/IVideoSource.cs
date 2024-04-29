using MovieSharp.Objects;
using SkiaSharp;

namespace MovieSharp;

public interface IVideoSource : IDisposable
{
    int FrameCount { get; }
    double FrameRate { get; }
    double Duration { get; }
    Coordinate Size { get; }
    PixelFormat PixelFormat { get; }
    Memory<byte> MakeFrame(int frameId);
    int GetFrameId(double time);
}

public static class IVideoSourceExtensions
{
    public static Memory<byte> MakeFrameByTime(this IVideoSource src, double time)
    {
        var id = src.GetFrameId(time);
        return src.MakeFrame(id);
    }
}
