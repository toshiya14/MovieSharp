using MovieSharp.Exceptions;
using NAudio.Vorbis;
using NAudio.Wave;

namespace MovieSharp.Sources.Audios;

internal class NAudioFileSource : IAudioSource
{
    private readonly WaveStream source;

    public double Duration { get; }
    public string FileName { get; }

    public NAudioFileSource(string filename)
    {
        this.FileName = filename;
        var fi = new FileInfo(filename);
        if (!fi.Exists)
        {
            throw new MovieSharpException(MovieSharpErrorType.ResourceNotFound, $"Not found: {fi.FullName}");
        }
        if (fi.Extension.ToLower() == ".ogg")
        {
            var source = new VorbisWaveReader(filename);
            this.source = source;
            this.Duration = source.TotalTime.TotalSeconds;
        }
        else
        {
            var source = new AudioFileReader(filename);
            this.source = source;
            this.Duration = source.TotalTime.TotalSeconds;
        }
    }

    public ISampleProvider GetSampler()
    {
        //return (this.source as ISampleProvider)!;
        return new AudioFileReader(this.FileName);
    }

    public void Dispose()
    {
        this.source?.Dispose();
        GC.SuppressFinalize(this);
    }
}
