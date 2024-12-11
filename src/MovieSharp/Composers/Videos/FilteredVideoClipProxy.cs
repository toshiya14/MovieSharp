using MovieSharp.Debugs.Benchmarks;
using MovieSharp.Objects;
using MovieSharp.Skia.Surfaces;
using SkiaSharp;

namespace MovieSharp.Composers.Videos;

internal class FilteredVideoClipProxy : IVideoClip, IFilteredVideoClip
{
    private readonly IVideoClip baseclip;
    private readonly SKPaint paint;

    public Coordinate Size => this.baseclip.Size;

    public double Duration => this.baseclip.Duration;

    private ISurfaceProxy? surface;

    public FilteredVideoClipProxy(IVideoClip baseclip)
    {
        this.baseclip = baseclip;
        this.paint = new SKPaint()
        {
            IsAntialias = true
        };
    }

    public void Dispose()
    {
        this.baseclip.Dispose();
        this.surface?.Dispose();
    }

    public IVideoClip ToClip()
    {
        return this;
    }

    public void Draw(SKCanvas canvas, SKPaint? paint, double time)
    {
        if (this.surface is null)
        {
            this.surface = new RasterSurface(new SKImageInfo(this.Size.X, this.Size.Y, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        }

        using var _ = PerformanceMeasurer.UseMeasurer("filtered-drawing");
        var (w, h) = this.baseclip.Size;


        //using var bmp = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        //using var cvs = new SKCanvas(bmp);
        this.surface.Canvas.Clear();
        this.baseclip.Draw(this.surface.Canvas, this.paint, time);
        this.surface.Canvas.Flush();
        using var img = this.surface.Snapshot();

        canvas.DrawImage(img, new SKPoint(0, 0));
    }

    public IFilteredVideoClip AddBlur(float sigmaX, float sigmaY)
    {
        if (this.paint.ImageFilter is null)
        {
            this.paint.ImageFilter = SKImageFilter.CreateBlur(sigmaX, sigmaY);
        }
        else
        {
            this.paint.ImageFilter = SKImageFilter.CreateMerge(
                this.paint.ImageFilter,
                SKImageFilter.CreateBlur(sigmaX, sigmaY)
            );
        }
        return this;
    }

    public IFilteredVideoClip AddColorTempOffset(float offset)
    {
        float[] matrix;
        if (offset < 0)
        {
            matrix = new float[] {
                1f - (0.39f * -offset), 0, 0, 0, 0,
                0, 1, 0, 0, 0,
                0, 0, 1, 0, 0,
                0, 0, 0, 1, 0
            };
        }
        else if (offset > 0)
        {
            matrix = new float[] {
                1, 0, 0, 0, 0,
                0, 1 - (0.08f * offset), 0, 0, 0,
                0, 0, 1 - (0.19f * offset), 0, 0,
                0, 0, 0, 1, 0
            };
        }
        else
        {
            // offset = 0, no colors would be changed.
            return this;
        }
        this.AppendColorFilter(matrix);
        return this;
    }

    public IFilteredVideoClip AddPresetFilter(PresetFilter preset)
    {
        var matrix = preset switch
        {
            PresetFilter.Retro => new float[]
            {
                0.5f, 0.5f, 0.5f, 0, 0,
                1 / 3f, 1 / 3f, 1 / 3f, 0, 0,
                0.25f, 0.25f, 0.25f, 0, 0,
                0, 0, 0, 1, 0
            },
            PresetFilter.Sepia => new float[]
            {
                0.3588f, 0.7044f, 0.1368f, 0, 0,
                0.2990f, 0.5870f, 0.1140f, 0, 0,
                0.2392f, 0.4696f, 0.0912f, 0, 0,
                0, 0, 0, 1, 0
            },
            PresetFilter.Nostalgic => new float[]
            {
                0.272f, 0.534f, 0.131f, 0, 0,
                0.349f, 0.686f, 0.168f, 0, 0,
                0.393f, 0.769f, 0.189f, 0, 0,
                0, 0, 0, 1, 0,
            },
            PresetFilter.Polaroid => new float[]
            {
                1.438f, -0.062f, -0.062f, 0, 0,
                -0.122f, 1.378f, -0.122f, 0, 0,
                -0.016f, -0.016f, 1.483f, 0, 0,
                -0.03f, 0.05f, 0.02f, 1, 0
            },
            PresetFilter.SwapRG => new float[]
            {
                0, 1, 0, 0, 0,
                1, 0, 0, 0, 0,
                0, 0, 1, 0, 0,
                0, 0, 0, 1, 0
            },
            PresetFilter.SwapRB => new float[]
            {
                0, 0, 1, 0, 0,
                0, 1, 0, 0, 0,
                1, 0, 0, 0, 0,
                0, 0, 0, 1, 0
            },
            PresetFilter.SwapGB => new float[]
            {
                1, 0, 0, 0, 0,
                0, 0, 1, 0, 0,
                0, 1, 0, 0, 0,
                0, 0, 0, 1, 0,
            },
            _ => throw new NotSupportedException("This preset of filter has not been supported yet: " + preset),
        };
        this.AppendColorFilter(matrix);
        return this;
    }

    private void AppendColorFilter(float[] matrix)
    {
        var filter = SKColorFilter.CreateColorMatrix(matrix);
        if (this.paint.ColorFilter is null)
        {
            this.paint.ColorFilter = filter;
        }
        else
        {
            this.paint.ColorFilter = SKColorFilter.CreateCompose(
                filter,
                this.paint.ColorFilter
            );
        }
    }

    public void Release()
    {
        this.baseclip.Release();
        this.surface?.Dispose();
        this.surface = null;
    }
}
