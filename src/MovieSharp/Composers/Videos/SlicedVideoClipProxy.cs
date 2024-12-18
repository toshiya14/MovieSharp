using MovieSharp.Objects;
using SkiaSharp;

namespace MovieSharp.Composers.Videos;

internal class SlicedVideoClipProxy : VideoClipBase
{
    private double StartTime { get; }
    private double EndTime { get; }

    public override double Duration => this.EndTime - this.StartTime;

    public SlicedVideoClipProxy(IVideoClip baseclip, double startTime, double endTime)
    {
        this.BaseClips = [baseclip];
        if (endTime < startTime)
        {
            throw new ArgumentException("The end time could not be earlier than the start time.");
        }
        this.StartTime = startTime;
        this.EndTime = endTime;
    }

    public override void Draw(SKCanvas canvas, SKPaint? paint, double time)
    {
        var realTime = time + this.StartTime;
        if (realTime > this.StartTime && realTime <= this.EndTime)
        {
            base.Draw(canvas, paint, realTime);
        }
    }
}
