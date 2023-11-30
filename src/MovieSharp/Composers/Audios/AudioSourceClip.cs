using NAudio.Wave;

namespace MovieSharp.Composers.Audios;

internal class AudioSourceClip : IAudioClip
{
    private readonly IAudioSource source;
    public double Duration => this.source.Duration;

    public int Channels => this.source.GetSampler().WaveFormat.Channels;

    public int SampleRate => this.source.GetSampler().WaveFormat.SampleRate;

    public AudioSourceClip(IAudioSource source)
    {
        this.source = source;
    }

    public ISampleProvider? GetSampler()
    {
        return this.source.GetSampler();
    }

    public void Dispose()
    {
        this.source.Dispose();
        GC.SuppressFinalize(this);
    }
}
