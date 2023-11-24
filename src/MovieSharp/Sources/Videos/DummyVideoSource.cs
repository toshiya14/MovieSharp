using MovieSharp.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp.Sources.Videos;

internal class DummyVideoSource : IVideoSource
{
    public long FrameCount => (long)(Duration * FrameRate);

    public double FrameRate { get; }

    public double Duration { get; }

    public Coordinate Size { get; }

    public PixelFormat PixelFormat { get; }

    private readonly RGBAColor? background;
    private Memory<byte>? buffer;

    public DummyVideoSource(RGBAColor? background, PixelFormat pixfmt, (int, int) size, double frameRate, double duration)
    {
        this.background = background;
        PixelFormat = pixfmt;
        Size = new Coordinate(size);
        FrameRate = frameRate;
        Duration = duration;
    }

    private Memory<byte>? MakeFrame()
    {
        if (background is null)
        {
            return null;
        }

        var bytesEachColor = PixelFormat.BitsEachColor / 8;
        var pixel = new byte[bytesEachColor];
        for (var i = 0; i < PixelFormat.ComponentsOrder.Length; i++)
        {
            var p = PixelFormat.ComponentsOrder[i];
            switch (p)
            {
                case 'r': pixel[i] = background.Red; break;
                case 'g': pixel[i] = background.Green; break;
                case 'b': pixel[i] = background.Blue; break;
                case 'a': pixel[i] = background.Alpha; break;
            }
        }

        var (w, h) = Size;
        var pixelsCount = w * h * bytesEachColor;
        var pixels = new byte[pixelsCount];
        for (var i = 0; i < pixelsCount; i += bytesEachColor)
        {
            for (var j = 0; j < bytesEachColor; j++)
            {
                pixels[i + j] = pixel[j];
            }
        }
        var buffer = pixels.AsMemory();
        this.buffer = buffer;
        return buffer;
    }

    public Memory<byte>? MakeFrame(long frameIndex)
    {
        return buffer ?? MakeFrame();
    }

    public Memory<byte>? MakeFrameByTime(double t)
    {
        return buffer ?? MakeFrame();
    }

    public void Dispose() { 
        GC.SuppressFinalize(this);
    }
}
