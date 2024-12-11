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
    void MakeFrameById(SKBitmap bitmap, int frameId);
    int GetFrameId(double time);
    void Close(bool cleanup);
}
