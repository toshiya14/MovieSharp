using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MovieSharp.Objects;
using SkiaSharp;

namespace MovieSharp.Sources.Videos;
internal class SkiaSequenceSource : IVideoSource
{
    public SkiaSequenceSource(string filepath)
    {
        this.Stream = File.OpenRead(filepath);
        this.Codec = SKCodec.Create(this.Stream);
        this.Duration = this.Codec.FrameInfo.Sum(x => x.Duration / 1000.0);
        var size = this.Codec.Info.Size;
        this.Size = new Coordinate(size.Width, size.Height);
        this.Frames = new SKBitmap[this.FrameCount];

        var imageInfo = new SKImageInfo(size.Width, size.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        for (var i = 0; i < this.FrameCount; i++)
        {
            var bitmap = new SKBitmap(imageInfo);
            var opt = new SKCodecOptions(i);
            this.Codec.GetPixels(imageInfo, bitmap.GetPixels(), opt);
            this.Frames[i] = bitmap;
        }
    }

    public int FrameCount => this.Codec.FrameCount;

    public double FrameRate => this.FrameCount / this.Duration;

    public double Duration { get; }

    public Coordinate Size { get; }

    public PixelFormat PixelFormat => PixelFormat.RGBA32;

    private SKCodec Codec { get; }

    private SKBitmap[] Frames { get; }

    private Stream Stream { get; }

    public void Dispose()
    {
        foreach (var frame in this.Frames)
        {
            frame.Dispose();
        }
        this.Stream.Dispose();
        this.Codec.Dispose();
        GC.SuppressFinalize(this);
    }

    public SKBitmap? MakeFrameById(int frameId)
    {
        var fid = frameId % this.Codec.FrameCount;
        return this.Frames[fid];
    }
    public int GetFrameId(double time) => (int)(this.FrameRate * time + 0.000001);

    public SKBitmap? MakeFrameByTime(double time)
    {
        var frameId = this.GetFrameId(time);
        return this.MakeFrameById(frameId);
    }
}
