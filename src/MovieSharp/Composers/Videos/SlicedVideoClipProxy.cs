using MovieSharp.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp.Composers.Videos;

public class SlicedVideoClipProxy : IVideoClip
{
    private readonly IVideoClip baseclip;
    private double StartTime { get; }
    private double EndTime { get; }

    public Coordinate Size => this.baseclip.Size;

    public double Duration => this.EndTime - this.StartTime;

    public SlicedVideoClipProxy(IVideoClip baseclip, double startTime, double endTime)
    {
        this.baseclip = baseclip;
        this.StartTime = startTime;
        this.EndTime = endTime;
    }

    public void Draw(SKCanvas canvas, SKPaint? paint, double time)
    {
        var realTime = time + this.StartTime;
        if (realTime > this.StartTime && realTime <= this.EndTime)
        {
            this.baseclip.Draw(canvas, paint, realTime);
        }
    }

    public void Dispose() {
        this.baseclip.Dispose();
        GC.SuppressFinalize(this);
    }
}
