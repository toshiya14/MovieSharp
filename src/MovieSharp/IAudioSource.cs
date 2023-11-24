using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp;

public interface IAudioSource : IDisposable
{
    double Duration { get; }
    ISampleProvider GetSampler();
}
