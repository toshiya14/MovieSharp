using MovieSharp.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
