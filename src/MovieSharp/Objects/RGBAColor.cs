using SkiaSharp;

namespace MovieSharp.Objects;

public record RGBAColor(byte Red, byte Green, byte Blue, byte Alpha)
{
    public SKColor ToSKColor()
    {
        return new SKColor(this.Red, this.Green, this.Blue, this.Alpha);
    }

    public static RGBAColor Black => new(0x00, 0x00, 0x00, 0xff);
}
