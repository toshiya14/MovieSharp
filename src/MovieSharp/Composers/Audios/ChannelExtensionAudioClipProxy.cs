using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp.Composers.Audios;

internal class ChannelExtensionAudioClipProxy : IAudioClip
{
    private readonly IAudioClip baseclip;
    private readonly int channels;

    public double Duration => this.baseclip.Duration;

    public int Channels => this.channels;

    public int SampleRate => this.baseclip.SampleRate;

    public ChannelExtensionAudioClipProxy(IAudioClip baseclip, int channels)
    {
        this.baseclip = baseclip;
        this.channels = channels;
    }

    public ISampleProvider GetSampler()
    {
        var baseChannels = this.baseclip.GetSampler().WaveFormat.Channels;

        if (baseChannels == channels) {
            // No need for conversion.
            return this.baseclip.GetSampler();
        }

        switch (this.channels)
        {
            case 1:
                // Stereo -> Mono
                var mono = new StereoToMonoSampleProvider(this.baseclip.GetSampler());
                return mono;

            case 2:
                // Mono -> Stereo
                var stereo = new MonoToStereoSampleProvider(this.baseclip.GetSampler());
                return stereo;

            default:
                throw new ArgumentException("Currently only support 1(mono) and 2(stereo).");
        }
    }

    public void Dispose()
    {
        this.baseclip.Dispose();
        GC.SuppressFinalize(this);
    }
}
