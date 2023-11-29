namespace MovieSharp.Objects.Subtitles;

public class TimelineItem
{
    public string Key { get; set; } = Ulid.NewUlid().ToString();
    public double Start { get; set; }
    public double End { get; set; }
    public Anchor Anchor { get; set; } = Anchor.Center;
    public float LineSpacing { get; set; } = 0;
    public TextAlign TextAlign { get; set; } = TextAlign.Center;
    public List<TextRun> Contents { get; set; } = new List<TextRun>();
}
