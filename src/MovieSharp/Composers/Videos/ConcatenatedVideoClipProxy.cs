using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MovieSharp.Objects;
using SkiaSharp;

namespace MovieSharp.Composers.Videos;
internal class ConcatenatedVideoClipProxy : VideoClipBase
{
    public override double Duration => this.BaseClips.Sum(x => x.Duration);

    public ConcatenatedVideoClipProxy(params IEnumerable<IVideoClip> baseclips)
    {
        foreach (var clip in baseclips)
        {
            if (clip is ConcatenatedVideoClipProxy concat)
            {
                foreach (var bc in concat.BaseClips)
                {
                    this.BaseClips.Add(bc);
                }
            }
            else
            {
                this.BaseClips.Add(clip);
            }
        }
    }

    protected override IEnumerable<(int clipIndex, IVideoClip clip, double realtime)> PickupDrawingClips(double time)
    {
        if (time > this.Duration || this.BaseClips.Count == 0)
        {
            // Do not draw frames not in this clip.
            return [];
        }

        var start = 0.0;
        for (var i = 0; i < this.BaseClips.Count; i++)
        {
            var end = start + this.BaseClips[i].Duration;
            if (time >= start && time <= end)
            {
                return [(i, this.BaseClips[i], time - start)];
            }
            start = end;
        }

        return [];
    }
}
