using MovieSharp.Objects.Subtitles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp.Tools;

public class SubtitleTimelineBuilder
{
    private readonly List<TimelineItem> items = new();

    public SimpleSubtitleTimelineContext AddSimple(double start, double end, string text)
    {
        var run = new TextRun(text);
        var item = new TimelineItem()
        {
            Start = start,
            End = end,
            Contents = new List<TextRun>() { run }
        };
        this.items.Add(item);
        return new SimpleSubtitleTimelineContext(item, run.Font);
    }

    public ComplexSubtitleTimelineContext AddComplex(double start, double end) {
        var item = new TimelineItem()
        {
            Start = start,
            End = end,
        };
        this.items.Add(item);
        return new ComplexSubtitleTimelineContext(item);
    }

    public List<TimelineItem> Make() {
        return this.items;
    }
}
