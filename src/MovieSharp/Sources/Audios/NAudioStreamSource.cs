using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MovieSharp.Exceptions;
using NAudio.Vorbis;
using NAudio.Wave;

namespace MovieSharp.Sources.Audios;
internal class NAudioStreamSource : IAudioSource
{
    private readonly StreamMediaFoundationReader source;

    public double Duration { get; }

    public NAudioStreamSource(Stream basestream)
    {
        this.source = new StreamMediaFoundationReader(basestream);
        this.Duration = this.source.TotalTime.TotalSeconds;
    }

    public ISampleProvider GetSampler()
    {
        return this.source.ToSampleProvider();
    }

    public void Dispose()
    {
        this.source?.Dispose();
        GC.SuppressFinalize(this);
    }
}
