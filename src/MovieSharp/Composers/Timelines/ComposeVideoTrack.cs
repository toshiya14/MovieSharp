namespace MovieSharp.Composers.Timelines;

public record ComposeVideoTrack
{
    public double Time { get; }
    public IVideoClip Clip { get; }
    public double DrawingOrder { get; }

    public ComposeVideoTrack(double time, IVideoClip clip, double drawingOrder = 0)
    {
        this.Time = time;
        this.Clip = clip;
        this.DrawingOrder = drawingOrder;
    }
}
