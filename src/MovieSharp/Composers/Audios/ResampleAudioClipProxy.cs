using NAudio.Wave;

namespace MovieSharp.Composers.Audios;

internal class ResampleAudioClipProxy : IAudioClip
{
    private readonly IAudioClip baseclip;

    public double Duration => this.baseclip.Duration;

    public int Channels => this.baseclip.Channels;

    public int SampleRate { get; set; }

    public ResampleAudioClipProxy(IAudioClip baseclip, int samplerate)
    {
        this.baseclip = baseclip;
        this.SampleRate = samplerate;
    }

    public ISampleProvider? GetSampler()
    {
        var sampler = this.baseclip.GetSampler();
        if (sampler == null) {
            return null;
        }

        var rsp = new MediaFoundationResampler(sampler.ToWaveProvider(), this.SampleRate);

        return rsp.ToSampleProvider();
    }

    public void Dispose()
    {
        this.baseclip.Dispose();
        GC.SuppressFinalize(this);
    }
}
