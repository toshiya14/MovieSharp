using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MovieSharp.Objects;
using SkiaSharp;

namespace MovieSharp.Composers.Videos;
internal class ZeroVideoClip : VideoClipBase
{
    public override Coordinate Size { get; }

    public override double Duration => 0.0;

    public ZeroVideoClip(int width, int height)
    {
        this.Size = new Coordinate(width, height);
    }

    public override void Draw(SKCanvas canvas, SKPaint? paint, double time)
    {
        // Do nothing.
    }
}
