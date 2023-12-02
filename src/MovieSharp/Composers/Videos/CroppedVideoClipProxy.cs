using MovieSharp.Debugs.Benchmarks;
using MovieSharp.Objects;
using MovieSharp.Skia.Surfaces;
using SkiaSharp;

namespace MovieSharp.Composers.Videos;

internal class CroppedVideoClipProxy : IVideoClip
{
    private readonly IVideoClip baseclip;
    private readonly RectBound croparea;

    public Coordinate Size { get; private set; }

    public double Duration => this.baseclip.Duration;

    public ISurfaceProxy surface;

    public CroppedVideoClipProxy(IVideoClip baseclip, RectBound croparea)
    {
        this.baseclip = baseclip;
        this.croparea = croparea;
        this.Size = new Coordinate(croparea.Width, croparea.Height);
        this.surface = new RasterSurface(new SKImageInfo(this.Size.X, this.Size.Y, SKColorType.Rgba8888, SKAlphaType.Unpremul));
    }

    public void Draw(SKCanvas canvas, SKPaint? paint, double time)
    {
        var pm = PerformanceMeasurer.GetCurrentClassMeasurer();
        using var _ = pm.UseMeasurer("cropped-drawing");

        this.surface.Canvas.Clear();
        this.baseclip.Draw(this.surface.Canvas, paint, time);
        var srcrect = new SKRect(this.croparea.Left, this.croparea.Top, this.croparea.Right, this.croparea.Bottom);
        var tarrect = new SKRect(0, 0, this.croparea.Width, this.croparea.Height);
        this.surface.Canvas.Flush();
        using var img = this.surface.Snapshot();

        canvas.DrawImage(img, srcrect, tarrect);
    }

    public void Dispose()
    {
        this.baseclip.Dispose();
        this.surface.Dispose();
        GC.SuppressFinalize(this);
    }
}
