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
    SKBitmap? MakeFrameByTime(double t);
    SKBitmap? MakeFrame(int frameIndex);
}
