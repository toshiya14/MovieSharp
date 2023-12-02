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

    private readonly RGBAColor? background;
    private SKImage? snapshot;

    public DummyVideoSource(RGBAColor? background, PixelFormat pixfmt, (int, int) size, double frameRate, double duration)
    {
        this.background = background;
        this.PixelFormat = pixfmt;
        this.Size = new Coordinate(size);
        this.FrameRate = frameRate;
        this.Duration = duration;
        using var surface = new RasterSurface(new SKImageInfo(this.Size.X, this.Size.Y, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        if (background is not null)
        {
            surface.Canvas.Clear(background.ToSKColor());
            this.snapshot = surface.Snapshot();
        }
    }

    private SKImage? MakeFrame()
    {
        return this.snapshot;
    }

    public SKImage? MakeFrame(int frameIndex)
    {
        return this.snapshot;
    }

    public SKImage? MakeFrameByTime(double t)
    {
        return this.snapshot;
    }

    public void Dispose()
    {
        this.snapshot?.Dispose();
        GC.SuppressFinalize(this);
    }
}
