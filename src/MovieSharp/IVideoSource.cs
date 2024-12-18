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
    void DrawFrame(SKCanvas canvas, SKPaint? paint, int frameId, (int x, int y) position);
    int GetFrameId(double time);
    void Close(bool cleanup);
}
