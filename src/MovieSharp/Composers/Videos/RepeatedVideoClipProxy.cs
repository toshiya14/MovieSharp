using MovieSharp.Objects;
using SkiaSharp;

namespace MovieSharp.Composers.Videos;

internal class RepeatedVideoClipProxy : VideoClipBase
{
    public override double Duration { get; }
    public RepeatedVideoClipProxy(IVideoClip baseclip, double duration)
    {
        this.BaseClips = [baseclip];
        if (duration <= 0)
        {
            throw new ArgumentException("The new duration for repeated video could not be 0 or negative.");
        }
        this.Duration = duration;
    }

    public override void Draw(SKCanvas canvas, SKPaint? paint, double time)
    {
        var baseclip = this.BaseClips[0];

        if (baseclip.Duration == 0) {
            // Do not try to draw any duration=0 clip.
            return;
        }

        var realTime = time;
        while (realTime >= baseclip.Duration)
        {
            realTime -= baseclip.Duration;
        }
        base.Draw(canvas, paint, realTime);
    }
}
