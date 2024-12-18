using MovieSharp.Objects;
using SkiaSharp;

namespace MovieSharp;
public interface IComposeCanvas
{
    ReadOnlyMemory<byte>? SeekAndRead(long frameIndex);
    long Position { get; }
    double FrameRate { get; }
    double Duration { get; }
    Coordinate Size { get; }
    SKImageInfo ImageInfo { get; }
}
