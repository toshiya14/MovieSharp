using MovieSharp.Objects.Subtitles;
using MovieSharp.Objects.Subtitles.Drawings;
using MovieSharp.Tools;

namespace MovieSharp;

public interface ISubtitleSource
{
    void AppendTimeline(TimelineItem item);
    void From(SubtitleTimelineBuilder stb);
    IVideoSource AsVideoSource();
}
