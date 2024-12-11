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
        this.FilePath = filepath;
    }

    private void LoadFrames()
    {
        using var stream = File.OpenRead(this.FilePath);
        using var codec = SKCodec.Create(stream);
        var duration = codec.FrameInfo.Sum(x => x.Duration / 1000.0);
        var size = codec.Info.Size;
        var frameCount = codec.FrameCount;
        this.Size = new Coordinate(size.Width, size.Height);

        this.Frames = new SKBitmap[frameCount];
        var imageInfo = new SKImageInfo(size.Width, size.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        for (var i = 0; i < frameCount; i++)
        {
            var bitmap = new SKBitmap(imageInfo);
            var opt = new SKCodecOptions(i);
            codec.GetPixels(imageInfo, bitmap.GetPixels(), opt);
            this.Frames[i] = bitmap;
        }

        this.FrameCount = frameCount;
        this.Duration = duration;
        this.FrameRate = frameCount / duration;
    }

    public double Duration { get; private set; } = 0;

    public int FrameCount { get; private set; } = 0;

    public Coordinate Size { get; private set; } = new Coordinate(0, 0);

    public PixelFormat PixelFormat => PixelFormat.RGBA32;


    private SKBitmap[]? Frames { get; set; }

    public string FilePath { get; }
    public double FrameRate { get; private set; }

    public void Close(bool cleanup)
    {
        if (this.Frames is not null)
        {
            foreach (var frame in this.Frames)
            {
                frame.Dispose();
            }
        }
        this.Frames = null;
    }

    public void Dispose()
    {
        if (this.Frames is not null)
        {
            foreach (var frame in this.Frames)
            {
                frame.Dispose();
            }
        }
        GC.SuppressFinalize(this);
    }
    public int GetFrameId(double time) => (int)(this.FrameRate * time + 0.000001);

    public void MakeFrameById(SKBitmap frame, int frameId)
    {
        if (this.Frames is null)
        {
            this.LoadFrames();
        }

        var fid = frameId % this.FrameCount;
        this.Frames![fid].CopyTo(frame);
    }
}
