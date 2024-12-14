using MovieSharp.Objects;
using MovieSharp.Skia.Surfaces;
using SkiaSharp;

namespace MovieSharp.Sources.Videos;

internal class DummyVideoSource : IVideoSource
{
    public int FrameCount => (int)(this.Duration * this.FrameRate);

    public double FrameRate { get; }

    public double Duration { get; }

    private readonly RGBAColor background;

    public Coordinate Size { get; }

    public PixelFormat PixelFormat { get; }

    private SKBitmap? snapshot;

    public DummyVideoSource(RGBAColor background, PixelFormat pixfmt, (int, int) size, double frameRate, double duration)
    {
        this.PixelFormat = pixfmt;
        this.Size = new Coordinate(size);
        this.FrameRate = frameRate;
        this.Duration = duration;
        this.background = background;
    }

    public void GenImg() {
        using var surface = new RasterSurface(new SKImageInfo(this.Size.X, this.Size.Y, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        if (this.background is not null)
        {
            surface.Canvas.Clear(this.background.ToSKColor());
            using var img = surface.Snapshot();
            this.snapshot = SKBitmap.FromImage(img);
        }
    }

    public int GetFrameId(double time) => (int)(this.FrameRate * time + 0.000001);

    public void DrawFrame(SKCanvas cvs, int frameIndex, (int x, int y) position)
    {
        if (this.snapshot is null) {
            this.GenImg();
        }
        cvs.DrawBitmap(this.snapshot, new SKPoint(position.x, position.y));
    }

    public void Dispose()
    {
        this.snapshot?.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Close(bool cleanup)
    {
        this.snapshot?.Dispose();
        this.snapshot = null;
    }
}
