﻿using NAudio.Wave;

namespace MovieSharp.Composers.Audios;
internal class RepeatAudioClipProxy : IAudioClip
{
    private readonly IAudioClip baseclip;

    public double Duration { get; }

    public int Channels => this.baseclip.Channels;

    public int SampleRate => this.baseclip.SampleRate;

    public RepeatAudioClipProxy(IAudioClip baseclip, double duration)
    {
        this.baseclip = baseclip;
        this.Duration = duration;
    }

    public ISampleProvider GetSampler()
    {
        var sampler = this.baseclip.GetSampler();
        var restTime = this.Duration - this.baseclip.Duration;
        while (restTime > 0)
        {
            if (restTime >= this.baseclip.Duration)
            {
                sampler = sampler.FollowedBy(this.baseclip.GetSampler());
                restTime -= this.baseclip.Duration;
            }
            else
            {
                sampler = sampler.FollowedBy(TimeSpan.FromSeconds(restTime), this.baseclip.GetSampler());
            }
        }
        return sampler;
    }

    public void Dispose()
    {
        this.baseclip.Dispose();
        GC.SuppressFinalize(this);
    }
}
