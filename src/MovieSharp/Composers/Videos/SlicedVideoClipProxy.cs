using MovieSharp.Objects;
using SkiaSharp;

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
        if (endTime < startTime)
        {
            throw new ArgumentException("The end time could not be earlier than the start time.");
        }
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

    public void Dispose()
    {
        this.baseclip.Dispose();
        GC.SuppressFinalize(this);
    }
}
