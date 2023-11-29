using MovieSharp.Objects;
using SkiaSharp;

namespace MovieSharp.Composers.Videos;

internal class CroppedVideoClipProxy : IVideoClip
{
    private readonly IVideoClip baseclip;
    private readonly RectBound croparea;

    public Coordinate Size { get; private set; }

    public double Duration => this.baseclip.Duration;

    public CroppedVideoClipProxy(IVideoClip baseclip, RectBound croparea)
    {
        this.baseclip = baseclip;
        this.croparea = croparea;
        this.Size = new Coordinate(croparea.Width, croparea.Height);
    }

    public void Draw(SKCanvas canvas, SKPaint? paint, double time)
    {
        using var bmp = new SKBitmap(this.baseclip.Size.X, this.baseclip.Size.Y, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var cvs = new SKCanvas(bmp);
        this.baseclip.Draw(cvs, paint, time);
        var srcrect = new SKRect(this.croparea.Left, this.croparea.Top, this.croparea.Right, this.croparea.Bottom);
        var tarrect = new SKRect(0, 0, this.croparea.Width, this.croparea.Height);
        cvs.Flush();

        canvas.DrawBitmap(bmp, srcrect, tarrect);
    }

    public void Dispose()
    {
        this.baseclip.Dispose();
        GC.SuppressFinalize(this);
    }
}
