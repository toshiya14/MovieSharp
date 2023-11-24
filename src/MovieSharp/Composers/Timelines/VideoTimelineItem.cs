namespace MovieSharp.Composers.Timelines;

internal class VideoTimelineItem
{
    public double Time { get; }
    public IVideoClip FrameProvider { get; }

    public VideoTimelineItem(double time, IVideoClip frameProvider)
    {
        this.Time = time;
        this.FrameProvider = frameProvider;
    }
}
