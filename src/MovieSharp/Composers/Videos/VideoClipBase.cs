using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MovieSharp.Objects;
using SkiaSharp;

namespace MovieSharp.Composers.Videos;
internal abstract class VideoClipBase : IVideoClip
{
    public virtual Coordinate Size => new(this.BaseClips.Max(x => x.Size.X), this.BaseClips.Max(x => x.Size.Y));
    public virtual double Duration => this.BaseClips.Max(x => x.Duration);
    protected virtual List<IVideoClip> BaseClips { get; set; } = [];

    protected virtual IEnumerable<(int clipIndex, IVideoClip clip, double realtime)> PickupDrawingClips(double time)
    {
        if (time > this.Duration) {
            return [];
        }

        if (this.BaseClips.Count == 1) {
            return [(0, this.BaseClips[0], time)];
        }

        return this.BaseClips.Where(x => x.Duration >= time).Select((x, i) => (i, x, time));
    }

    protected virtual void DrawSingleClip(SKCanvas canvas, SKPaint? paint, double realtime, int clipIndex, IVideoClip clip)
    {
        clip.Draw(canvas, paint, realtime);
    }

    public virtual void Draw(SKCanvas canvas, SKPaint? paint, double time)
    {
        var cols = this.PickupDrawingClips(time);
        foreach (var (index, clip, realtime) in cols)
        {
            this.DrawSingleClip(canvas, paint, realtime, index, clip);
        }
    }

    public virtual void Release()
    {
        foreach (var clip in this.BaseClips)
        {
            clip.Release();
        }
    }

    public virtual void Dispose()
    {
        this.Release();
        GC.SuppressFinalize(this);
    }
}
