using MovieSharp.Objects;
using MovieSharp.Objects.EncodingParameters;
using MovieSharp.Targets.Videos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp.Composers;

public interface ICompose : IAudioClip, IVideoClip
{
    /// <summary>
    /// Get the framerate of this compose.
    /// </summary>
    double FrameRate { get; }

    /// <summary>
    /// Set the region to render.
    /// If keep null, the whole compose would be used.(equals [0, compose.Duration])
    /// </summary>
    TimeRange? RenderRange { set; }

    /// <summary>
    /// Set the output of the final mp4.
    /// </summary>
    string OutputFile { set; }

    /// <summary>
    /// Set the temporary used audio output.
    /// If keep null, would use system temp folder.
    /// </summary>
    string? TempAudioFile { set; }

    event EventHandler<OnFrameWrittenEventArgs>? OnFrameWritten;
    event EventHandler<FFProgressData>? OnFrameEncoded;

    void PutVideo(IVideoClip video, double time);
    void PutAudio(IAudioClip audio, double time);
    void ComposeVideo(FFVideoParams? p = null);
    void ComposeAudio(NAudioParams? p = null);
    void Compose(FFVideoParams? vp = null, NAudioParams? ap = null);
}
