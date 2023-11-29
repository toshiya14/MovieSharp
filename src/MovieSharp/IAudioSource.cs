using NAudio.Wave;

namespace MovieSharp;

public interface IAudioSource : IDisposable
{
    double Duration { get; }
    ISampleProvider GetSampler();
}
