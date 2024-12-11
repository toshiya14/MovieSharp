using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MovieSharp.Objects;
using SkiaSharp;

namespace MovieSharp.Composers.Videos;
internal class ConcatenatedVideoClipProxy : IVideoClip
{

    private readonly IVideoClip baseclip1;
    private readonly IVideoClip baseclip2;

    public Coordinate Size { get; }


    public double Duration => this.baseclip1.Duration + this.baseclip2.Duration;

    public ConcatenatedVideoClipProxy(IVideoClip baseclip1, IVideoClip baseclip2)
    {
        this.baseclip1 = baseclip1;
        this.baseclip2 = baseclip2;
        var width = Math.Max(this.baseclip1.Size.X, this.baseclip2.Size.X);
        var height = Math.Max(this.baseclip1.Size.Y, this.baseclip2.Size.Y);
        this.Size = new Coordinate(width, height);
    }

    public void Draw(SKCanvas canvas, SKPaint? paint, double time)
    {
        if (time > this.Duration)
        {
            // Do not draw frames not in this clip.
            return;
        }
        if (time < this.baseclip1.Duration)
        {
            this.baseclip1.Draw(canvas, paint, time);
        }
        else
        {
            this.baseclip2.Draw(canvas, paint, time - this.baseclip1.Duration);
        }
    }

    public void Dispose()
    {
        this.baseclip1.Dispose();
        this.baseclip2.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Release()
    {
        this.baseclip1.Release();
        this.baseclip2.Release();
    }
}
