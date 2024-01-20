using IronSoftware.Drawing;
using MovieSharp.Objects.Subtitles;

namespace MovieSharp.Tools;

public class SubtitleTimelineBuilder
{
    private readonly List<TimelineItem> items = new();

    public FontDefinition DefaultFont { get; }

    public SubtitleTimelineBuilder(Font defaultFont)
    {
        this.DefaultFont = new FontDefinition(defaultFont);
    }

    public SimpleSubtitleTimelineContext AddSimple(double start, double end, string text)
    {
        var run = new TextRun(text) { Font = this.DefaultFont };
        var item = new TimelineItem()
        {
            Start = start,
            End = end,
            Contents = new List<TextRun>() { run }
        };
        this.items.Add(item);
        return new SimpleSubtitleTimelineContext(item, run.Font);
    }

    public ComplexSubtitleTimelineContext AddComplex(double start, double end)
    {
        var item = new TimelineItem()
        {
            Start = start,
            End = end,
        };
        this.items.Add(item);
        return new ComplexSubtitleTimelineContext(item, this.DefaultFont);
    }

    public List<TimelineItem> Make()
    {
        return this.items;
    }
}
