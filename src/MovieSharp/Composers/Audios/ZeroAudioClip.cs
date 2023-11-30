using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace MovieSharp.Composers.Audios;

internal class ZeroAudioClip : IAudioClip
{
    public double Duration => 0.0;

    public int Channels { get; }

    public int SampleRate { get; }

    public ZeroAudioClip(int channels, int sampleRate)
    {
        this.Channels = channels;
        this.SampleRate = sampleRate;
    }

    public void Dispose()
    {
        // Do nothing.
    }

    public ISampleProvider? GetSampler()
    {
        return null;
    }
}
