using MovieSharp.Objects;
using SkiaSharp;

namespace MovieSharp.Composers.Videos;
internal class SpeedChangedVideoClipProxy : IVideoClip
{
    private readonly IVideoClip baseclip;
    public Coordinate Size => this.baseclip.Size;

    public double Duration => this.baseclip.Duration / this.Speed;

    public double Speed { get; }

    public SpeedChangedVideoClipProxy(IVideoClip baseclip, double speed)
    {
        if (speed == 0)
        {
            throw new ArgumentException("Speed could not be `0`.");
        }
        this.baseclip = baseclip;
        this.Speed = speed;
    }

    public void Dispose()
    {
        this.baseclip.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Draw(SKCanvas canvas, SKPaint? paint, double time)
    {
        if (time > this.Duration)
        {
            // Do not draw frames not in this clip.
            return;
        }
        var realTime = time * this.Speed;
        this.baseclip.Draw(canvas, paint, realTime);
    }

    public void Release()
    {
        this.baseclip.Release();
    }
}
