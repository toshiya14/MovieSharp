using SkiaSharp;

namespace MovieSharp.Objects;

public class PixelFormat
{
    public string ComponentsOrder { get; set; } = string.Empty;
    public int BitsEachColor { get; set; }

    public PixelFormat(string comord, int bits)
    {
        this.ComponentsOrder = comord;
        this.BitsEachColor = bits;
    }

    public static PixelFormat RGB24 => new("rgb", 24);
    public static PixelFormat BGR24 => new("bgr", 24);
    public static PixelFormat RGBA32 => new("rgba", 32);
    public static PixelFormat BGRA32 => new("bgra", 32);
    public static PixelFormat ARGB32 => new("argb", 32);

    public SKColorType GetColorType()
    {
        if (this.ComponentsOrder == "rgba" && this.BitsEachColor == 32)
        {
            return SKColorType.Rgba8888;
        }
        else if (this.ComponentsOrder == "bgra" && this.BitsEachColor == 32)
        {
            return SKColorType.Bgra8888;
        }
        else
        {
            throw new ArgumentException($"SKColorType do not support {this.ComponentsOrder}:{this.BitsEachColor}.");
        }
    }

    public string ToFFPixfmt()
    {
        switch (this.ComponentsOrder)
        {
            case "rgb": return "rgb24";
            case "bgr": return "bgr24";
            case "rgba": return "rgba";
            case "bgra": return "bgra";
            case "argb": return "argb";
            default: throw new NotSupportedException("Unknown components order: " + this.ComponentsOrder);
        }
    }

    public static bool operator ==(PixelFormat left, PixelFormat right)
    {
        return left.ComponentsOrder.Equals(right.ComponentsOrder, StringComparison.OrdinalIgnoreCase) && left.BitsEachColor == right.BitsEachColor;
    }

    public static bool operator !=(PixelFormat left, PixelFormat right)
    {
        return !(left == right);
    }

    public override bool Equals(object? obj)
    {
        if (obj is PixelFormat pf)
        {
            return this == pf;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        throw new NotImplementedException();
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this);
    }
}
