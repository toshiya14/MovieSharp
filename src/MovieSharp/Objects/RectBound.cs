using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp.Objects;

public record struct RectBound(int Left, int Top, int Width, int Height)
{
    public readonly int Right => Left + Width;
    public readonly int Bottom => Top + Height;
}
