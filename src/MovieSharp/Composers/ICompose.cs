using MovieSharp.Objects;
using MovieSharp.Objects.EncodingParameters;
using MovieSharp.Targets.Videos;

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

    void PutVideo(double time, IVideoClip video);
    void PutAudio(double time, IAudioClip audio);
    void ComposeVideo(FFVideoParams? p = null);
    void ComposeAudio(NAudioParams? p = null);
    void Compose(FFVideoParams? vp = null, NAudioParams? ap = null);
    void UseMaxRenderRange();
}
