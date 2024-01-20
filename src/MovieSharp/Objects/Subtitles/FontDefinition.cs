using IronSoftware.Drawing;
using MovieSharp.Fonts;
using SkiaSharp;

namespace MovieSharp.Objects.Subtitles;

public class FontDefinition
{
    //public FontFamily Family { get; set; } = new FontFamily(FontSource.System, "Arial");
    //public float Size { get; set; }
    //public bool Bold { get; set; }
    public Font Font { get; set; }
    public RGBAColor Color { get; set; } = new RGBAColor(0, 0, 0, 0xff);
    public RGBAColor BorderColor { get; set; } = new RGBAColor(0xff, 0xff, 0xff, 0xff);
    public float BorderSize { get; set; } = 0;

    public FontDefinition(Font font)
    {
        this.Font = font;
    }

    private SKPaint CreateBasePaint(IFontManager<SKFont> fontman)
    {
        var font = fontman.CreateFont(this.Font);

        return new SKPaint()
        {
            IsAntialias = true,
            TextSize = font.Size,
            Typeface = font.Typeface,
        };
    }

    internal SKPaint CreateMeasurePaint(IFontManager<SKFont> fontman)
    {
        var paint = this.CreateBasePaint(fontman);
        paint.Color = this.Color.ToSKColor();
        paint.Style = SKPaintStyle.StrokeAndFill;
        paint.StrokeWidth = this.BorderSize;
        return paint;
    }

    internal SKPaint CreateStrokePaint(IFontManager<SKFont> fontman)
    {
        var paint = this.CreateBasePaint(fontman);
        paint.Color = this.BorderColor.ToSKColor();
        paint.IsStroke = true;
        paint.StrokeWidth = this.BorderSize;
        return paint;
    }

    internal SKPaint CreateFillPaint(IFontManager<SKFont> fontman)
    {
        var paint = this.CreateBasePaint(fontman);
        paint.Color = this.Color.ToSKColor();
        paint.Style = SKPaintStyle.Fill;
        return paint;
    }

    public FontDefinition Clone()
    {
        return new FontDefinition(new Font(this.Font.FamilyName, this.Font.Style, this.Font.Size))
        {
            Color = this.Color with { },
            BorderColor = this.BorderColor with { },
            BorderSize = this.BorderSize
        };
    }
}
