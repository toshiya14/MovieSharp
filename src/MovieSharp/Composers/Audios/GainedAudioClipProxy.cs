﻿using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace MovieSharp.Composers.Audios;

internal class GainedAudioClipProxy : IAudioClip
{
    private readonly IAudioClip baseclip;
    private readonly float gain;

    public double Duration => this.baseclip.Duration;

    public int Channels => this.baseclip.Channels;

    public int SampleRate => this.baseclip.SampleRate;

    public GainedAudioClipProxy(IAudioClip baseclip, float gain)
    {
        this.baseclip = baseclip;
        this.gain = gain;
    }

    public ISampleProvider GetSampler()
    {
        var vsp = new VolumeSampleProvider(this.baseclip.GetSampler())
        {
            Volume = this.gain
        };
        return vsp;
    }

    public void Dispose()
    {
        this.baseclip.Dispose();
        GC.SuppressFinalize(this);
    }
}
