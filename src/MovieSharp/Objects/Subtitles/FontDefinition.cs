using SkiaSharp;

namespace MovieSharp.Objects.Subtitles;

public class FontDefinition
{
    public string Family { get; set; } = string.Empty;
    public float Size { get; set; }
    public bool Bold { get; set; }
    public RGBAColor Color { get; set; } = new RGBAColor(0, 0, 0, 0xff);
    public RGBAColor BorderColor { get; set; } = new RGBAColor(0xff, 0xff, 0xff, 0xff);

    private SKPaint CreateBasePaint()
    {
        return new SKPaint()
        {
            IsAntialias = true,
            TextSize = this.Size,
            Typeface = SKTypeface.FromFamilyName(this.Family),
        };
    }

    public SKPaint CreateMeasurePaint()
    {
        var paint = this.CreateBasePaint();
        paint.Color = this.Color.ToSKColor();
        paint.Style = SKPaintStyle.StrokeAndFill;
        paint.StrokeWidth = this.Size / 12f;
        return paint;
    }

    public SKPaint CreateStrokePaint()
    {
        var paint = this.CreateBasePaint();
        paint.Color = this.BorderColor.ToSKColor();
        paint.IsStroke = true;
        paint.StrokeWidth = this.Size / 12f;
        return paint;
    }

    public SKPaint CreateFillPaint()
    {
        var paint = this.CreateBasePaint();
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
