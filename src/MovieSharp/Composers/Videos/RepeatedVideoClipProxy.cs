﻿using MovieSharp.Objects;
using SkiaSharp;

namespace MovieSharp.Composers.Videos;

public class RepeatedVideoClipProxy : IVideoClip
{
    private readonly IVideoClip baseclip;

    public Coordinate Size => this.baseclip.Size;

    public double Duration { get; }

    public RepeatedVideoClipProxy(IVideoClip baseclip, double duration)
    {
        if (duration < baseclip.Duration)
        {
            throw new ArgumentException($"Could not create an repeated video clip. The duration of the base clip: {baseclip.Duration}s, wanted: {duration}s");
        }
        this.baseclip = baseclip;
        this.Duration = duration;
    }

    public void Draw(SKCanvas canvas, SKPaint? paint, double time)
    {
        if (time > this.Duration) {
            // Do not draw frames not in this clip.
            return;
        }
        var realTime = time;
        while (realTime >= this.baseclip.Duration)
        {
            realTime -= this.baseclip.Duration;
        }
        this.baseclip.Draw(canvas, paint, realTime);
    }

    public void Dispose()
    {
        this.baseclip.Dispose();
        GC.SuppressFinalize(this);
    }
}
