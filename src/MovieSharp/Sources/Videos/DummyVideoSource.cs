using System.Buffers;
using MovieSharp.Objects;
using MovieSharp.Skia.Surfaces;
using SkiaSharp;

namespace MovieSharp.Sources.Videos;

internal class DummyVideoSource : IVideoSource
{
    public int FrameCount => (int)(this.Duration * this.FrameRate);

    public double FrameRate { get; }

    public double Duration { get; }

    public Coordinate Size { get; }

    public PixelFormat PixelFormat { get; }

    //private SKBitmap? snapshot;
    private readonly Memory<byte> framebuf;
    private readonly MemoryHandle framehandle;

    public unsafe DummyVideoSource(RGBAColor? background, PixelFormat pixfmt, (int, int) size, double frameRate, double duration)
    {
        this.PixelFormat = pixfmt;
        this.Size = new Coordinate(size);
        this.FrameRate = frameRate;
        this.Duration = duration;
        var imginfo = new SKImageInfo(this.Size.X, this.Size.Y, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var surface = new RasterSurface(imginfo);
        this.framebuf = new Memory<byte>(new byte[4 * this.Size.X * this.Size.Y]);
        this.framehandle = this.framebuf.Pin();
        if (background is not null)
        {
            surface.Canvas.Clear(background.ToSKColor());
            using var img = surface.Snapshot();
            var map = new SKPixmap(imginfo, (nint)this.framehandle.Pointer, imginfo.RowBytes);
            img.PeekPixels(map);
        }
    }

    public int GetFrameId(double time) => (int)(this.FrameRate * time + 0.000001);

    public Memory<byte> MakeFrame(int frameIndex)
    {
        return this.framebuf;
    }

    public void Dispose()
    {
        //this.snapshot?.Dispose();
        this.framehandle.Dispose();
        GC.SuppressFinalize(this);
    }
}
