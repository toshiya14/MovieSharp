using NAudio.Vorbis;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp.Sources.Audios;

internal class NAudioFileSource : IAudioSource
{
    private readonly WaveStream source;

    public double Duration { get; }

    public NAudioFileSource(string filename)
    {
        var fi = new FileInfo(filename);
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
        return (source as ISampleProvider)!;
    }

    public void Dispose() {
        this.source?.Dispose();
        GC.SuppressFinalize(this);
    }
}
