using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp.Composers.Audios;

internal class ResampleAudioClipProxy : IAudioClip
{
    private readonly IAudioClip baseclip;
    private readonly int samplerate;

    public double Duration => this.baseclip.Duration;

    public int Channels => this.baseclip.Channels;

    public int SampleRate => samplerate;

    public ResampleAudioClipProxy(IAudioClip baseclip, int samplerate) {
        this.baseclip = baseclip;
        this.samplerate = samplerate;
    }

    public ISampleProvider GetSampler() {
        var rsp = new MediaFoundationResampler(this.baseclip.GetSampler().ToWaveProvider(), this.samplerate);
        return rsp.ToSampleProvider();
    }

    public void Dispose() {
        this.baseclip.Dispose();
        GC.SuppressFinalize(this);
    }
}
