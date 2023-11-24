using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp.Composers.Audios;

internal class GainedAudioClipProxy : IAudioClip
{
    private readonly IAudioClip baseclip;
    private readonly float gain;

    public double Duration => this.baseclip.Duration;

    public int Channels => this.baseclip.Channels;

    public int SampleRate => this.baseclip.SampleRate;

    public GainedAudioClipProxy(IAudioClip baseclip, float gain) {
        this.baseclip = baseclip;
        this.gain = gain;
    }

    public ISampleProvider GetSampler() {
        var vsp = new VolumeSampleProvider(this.baseclip.GetSampler());
        vsp.Volume = this.gain;
        return vsp;
    }

    public void Dispose() {
        this.baseclip.Dispose();
        GC.SuppressFinalize(this);
    }
}
