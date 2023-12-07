using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MovieSharp.Objects;
using NAudio.Wave;
using SkiaSharp;

namespace MovieSharp.Composers.Audios;
internal class ConcatenatedAudioClipProxy : IAudioClip
{

    private readonly IAudioClip baseclip1;
    private readonly IAudioClip baseclip2;


    public double Duration => this.baseclip1.Duration + this.baseclip2.Duration;

    public int Channels => this.baseclip1.Channels;

    public int SampleRate => this.baseclip1.SampleRate;

    public ConcatenatedAudioClipProxy(IAudioClip baseclip1, IAudioClip baseclip2)
    {
        this.baseclip1 = baseclip1;
        this.baseclip2 = baseclip2;

        if (this.baseclip2.Channels != this.Channels)
        {
            this.baseclip2 = this.baseclip2.ChangeChannels(this.Channels);
        }
        if (this.baseclip2.SampleRate != this.SampleRate)
        {
            this.baseclip2 = this.baseclip2.Resample(this.SampleRate);
        }
    }

    public void Dispose()
    {
        this.baseclip1.Dispose();
        this.baseclip2.Dispose();
        GC.SuppressFinalize(this);
    }

    public ISampleProvider? GetSampler()
    {
        var sampler1 = this.baseclip1.GetSampler();
        var sampler2 = this.baseclip2.GetSampler();
        if (sampler1 is null)
        {
            return sampler2;
        }

        if (sampler2 is null)
        {
            return sampler1;
        }

        return this.baseclip1.GetSampler().FollowedBy(this.baseclip2.GetSampler());
    }
}
