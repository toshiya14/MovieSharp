using NAudio.Wave;
using NAudio.Wave.SampleProviders;

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

    public ISampleProvider? GetSampler()
    {
        var baseChannels = this.baseclip.Channels;

        if (baseChannels == this.channels)
        {
            // No need for conversion.
            return this.baseclip.GetSampler();
        }

        var sampler = this.baseclip.GetSampler();
        if (sampler == null)
        {
            return null;
        }

        switch (this.channels)
        {
            case 1:
                // Stereo -> Mono
                var mono = new StereoToMonoSampleProvider(sampler);
                return mono;

            case 2:
                // Mono -> Stereo
                var stereo = new MonoToStereoSampleProvider(sampler);
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
