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
    /// The duration.
    /// </summary>
    new double Duration { get; }

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

    /// <summary>
    /// Set output only contains audio.
    /// </summary>
    bool OnlyEncodeAudio { set; }

    /// <summary>
    /// Video encoding parameters for ffmpeg.
    /// </summary>
    FFVideoParams VideoParams { get; set; }

    /// <summary>
    /// Audio encoding parameters for NAudio.
    /// </summary>
    NAudioParams AudioParams { get; set; }

    event EventHandler<OnFrameWrittenEventArgs>? OnFrameWritten;
    event EventHandler<FFProgressData>? OnFrameEncoded;
    event EventHandler OnCancelled;
    event EventHandler OnCompleted;

    void PutVideo(double time, IVideoClip video, double renderOrder);
    void PutAudio(double time, IAudioClip audio);
    void Compose();
    void Cancel();
    void UseMaxRenderRange();
}
