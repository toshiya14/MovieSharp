namespace MovieSharp.Composers.Timelines;

internal class AudioTimelineItem
{
    public double Time { get; }
    public IAudioClip WaveProvider { get; }

    public AudioTimelineItem(double time, IAudioClip waveProvider)
    {
        this.Time = time;
        this.WaveProvider = waveProvider;
    }
}
