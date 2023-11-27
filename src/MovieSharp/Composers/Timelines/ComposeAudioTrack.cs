namespace MovieSharp.Composers.Timelines;

public record ComposeAudioTrack
{
    public double Time { get; }
    public IAudioClip Clip { get; }

    public ComposeAudioTrack(double time, IAudioClip waveProvider)
    {
        this.Time = time;
        this.Clip = waveProvider;
    }
}
