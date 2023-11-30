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

        var bc1chns = baseclip1.Channels;
        var bc1sr = baseclip1.SampleRate;
        if (this.baseclip2.Channels != bc1chns)
        {
            this.baseclip2 = this.baseclip2.ChangeChannels(bc1chns);
        }
        if (this.baseclip2.SampleRate != bc1sr)
        {
            this.baseclip2 = this.baseclip2.Resample(bc1sr);
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
