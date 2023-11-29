using MovieSharp.Objects;

namespace MovieSharp;

public interface IVideoSource : IDisposable
{
    long FrameCount { get; }
    double FrameRate { get; }
    double Duration { get; }
    Coordinate Size { get; }
    PixelFormat PixelFormat { get; }
    Memory<byte>? MakeFrameByTime(double t);
    Memory<byte>? MakeFrame(long frameIndex);
}
