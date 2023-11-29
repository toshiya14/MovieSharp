using NAudio.Wave;

namespace MovieSharp.Sources.Audios;

internal class NAudioEmptySource : IAudioSource
{

    public double Duration { get; }

    private readonly SilenceProvider provider;

    public NAudioEmptySource(double duration, WaveFormat format)
    {
        this.Duration = duration;
        this.provider = new SilenceProvider(format);
    }

    public void Dispose()
    {
        // Do nothing.
    }

    public ISampleProvider GetSampler()
    {
        return this.provider.ToSampleProvider();
    }
}