using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp.Objects;

public record RGBAColor(byte Red, byte Green, byte Blue, byte Alpha) {
    public SKColor ToSKColor() { 
        return new SKColor(Red, Green, Blue, Alpha);
    }

    public static RGBAColor Black => new(0x00, 0x00, 0x00, 0xff);
}
