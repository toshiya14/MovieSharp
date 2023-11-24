using MovieSharp.Objects.Subtitles;
using MovieSharp.Objects.Subtitles.Drawings;
using MovieSharp.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp;

public interface ISubtitleSource
{
    void AppendSubtitle(TimelineItem item);
    void From(SubtitleTimelineBuilder stb);
    IVideoSource AsVideoSource();

#if DEBUG
    DrawingTextBox? GetLastTextBox();
#endif
}
