using MovieSharp.Objects.Subtitles;

namespace MovieSharp.Tools
{
    public class SimpleSubtitleTimelineContext
    {
        readonly TimelineItem timeline;
        readonly FontDefinition font;

        public SimpleSubtitleTimelineContext(TimelineItem timeline, FontDefinition font)
        {
            this.timeline = timeline;
            this.font = font;
        }

        public SimpleSubtitleTimelineContext WithTimeline(Action<TimelineItem> action)
        {
            action(this.timeline);
            return this;
        }

        public SimpleSubtitleTimelineContext WithFont(Action<FontDefinition> action)
        {
            action(this.font);
            return this;
        }
    }

    public class ComplexSubtitleTimelineContext
    {
        readonly TimelineItem timeline;
        readonly FontDefinition defaultFont;

        public ComplexSubtitleTimelineContext(TimelineItem timeline)
        {
            this.timeline = timeline;
            this.defaultFont = new FontDefinition();
        }

        public ComplexSubtitleTimelineContext WithTimeline(Action<TimelineItem> action)
        {
            action(this.timeline);
            return this;
        }

        public ComplexSubtitleTimelineContext UseFont(Action<FontDefinition> action)
        {
            action(this.defaultFont);
            return this;
        }

        public ComplexSubtitleTimelineContext AddRun(string text)
        {
            var run = new TextRun(text) { Font = this.defaultFont.Clone() };
            this.timeline.Contents.Add(run);
            return this;
        }
    }
}
