using SkiaSharp;

namespace MovieSharp.Objects.Subtitles;

public class FontDefinition
{
    public FontFamily Family { get; set; } = new FontFamily(FontSource.System, "Arial");
    public float Size { get; set; }
    public bool Bold { get; set; }
    public RGBAColor Color { get; set; } = new RGBAColor(0, 0, 0, 0xff);
    public RGBAColor BorderColor { get; set; } = new RGBAColor(0xff, 0xff, 0xff, 0xff);
    public float BorderSize { get; set; } = 0;

    private SKPaint CreateBasePaint(FontCache cache)
    {
        return new SKPaint()
        {
            IsAntialias = true,
            TextSize = this.Size,
            Typeface = this.Family.Source is FontSource.System ? SKTypeface.FromFamilyName(this.Family.Name) : cache.Get(this.Family.Name!),
        };
    }

    internal SKPaint CreateMeasurePaint(FontCache cache)
    {
        var paint = this.CreateBasePaint(cache);
        paint.Color = this.Color.ToSKColor();
        paint.Style = SKPaintStyle.StrokeAndFill;
        paint.StrokeWidth = this.BorderSize;
        return paint;
    }

    internal SKPaint CreateStrokePaint(FontCache cache)
    {
        var paint = this.CreateBasePaint(cache);
        paint.Color = this.BorderColor.ToSKColor();
        paint.IsStroke = true;
        paint.StrokeWidth = this.BorderSize;
        return paint;
    }

    internal SKPaint CreateFillPaint(FontCache cache)
    {
        var paint = this.CreateBasePaint(cache);
        paint.Color = this.Color.ToSKColor();
        paint.Style = SKPaintStyle.Fill;
        return paint;
    }

    public FontDefinition Clone()
    {
        return new FontDefinition()
        {
            Family = this.Family,
            Size = this.Size,
            Bold = this.Bold,
            Color = this.Color with { },
            BorderColor = this.BorderColor with { }
        };
    }
}
