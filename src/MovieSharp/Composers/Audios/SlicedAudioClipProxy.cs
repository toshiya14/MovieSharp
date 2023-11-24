using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp.Composers.Audios;

internal class SlicedAudioClipProxy : IAudioClip
{
    private readonly IAudioClip baseclip;

    public double StartTime { get; }
    public double EndTime { get; }

    public double Duration => this.EndTime - this.StartTime;

    public int Channels => this.baseclip.Channels;

    public int SampleRate => this.baseclip.SampleRate;

    public SlicedAudioClipProxy(IAudioClip baseclip, double start, double end)
    {
        this.baseclip = baseclip;
        if(start < 0 || end > this.baseclip.Duration)
        {
            throw new ArgumentException("The specified start/end is out of the range of the duration of the baseclip.");
        }
        this.StartTime = start;
        this.EndTime = end;
    }

    public ISampleProvider GetSampler()
    {
        var s = this.baseclip.GetSampler();
        return s.Skip(TimeSpan.FromSeconds(this.StartTime)).Take(TimeSpan.FromSeconds(this.Duration));
    }
    public void Dispose()
    {
        this.baseclip.Dispose();
        GC.SuppressFinalize(this);
    }
}
