using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MovieSharp.Objects;
using SkiaSharp;

namespace MovieSharp.Sources.Videos;
internal class SkiaSequenceSource : IVideoSource
{
    public unsafe SkiaSequenceSource(string filepath)
    {
        using var stream = File.OpenRead(filepath);
        using var codec = SKCodec.Create(stream);
        this.Duration = codec.FrameInfo.Sum(x => x.Duration / 1000.0);
        var size = codec.Info.Size;
        this.Size = new Coordinate(size.Width, size.Height);
        this.Frames = new Memory<byte>[this.FrameCount];
        this.FrameCount = codec.FrameCount;

        var imageInfo = new SKImageInfo(size.Width, size.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        for (var i = 0; i < this.FrameCount; i++)
        {
            using var bitmap = new SKBitmap(imageInfo);
            var opt = new SKCodecOptions(i);
            var pixels = bitmap.GetPixels();
            codec.GetPixels(imageInfo, pixels, opt);
            var span = new Span<byte>(pixels.ToPointer(), bitmap.Info.BytesSize);
            var mem = span.ToArray().AsMemory();
            this.Frames[i] = mem;
        }
    }

    public int FrameCount { get; }

    public double FrameRate => this.FrameCount / this.Duration;

    public double Duration { get; }

    public Coordinate Size { get; }

    public PixelFormat PixelFormat => PixelFormat.RGBA32;

    private Memory<byte>[] Frames { get; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public unsafe Memory<byte> MakeFrame(int frameId)
    {
        var fid = frameId % this.FrameCount;
        return this.Frames[fid];
    }
    public int GetFrameId(double time) => (int)(this.FrameRate * time + 0.000001);
}
