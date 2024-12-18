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
    void DrawFrame(SKCanvas canvas, SKPaint? paint, long frameId, (int x, int y) position);
    long GetFrameId(double time);
    void Close(bool cleanup);
}

public interface ICachedVideoSource : IVideoSource
{
    void UseCache(int maxPreloadFrames);
    bool CanPreload(long frameId);
    void Preload(long frameId);
    ReadOnlyMemory<byte>? WaitFrame(long frameId);
    void Release();
}