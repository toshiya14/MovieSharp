using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MovieSharp.Objects.Subtitles;
using MovieSharp.Objects.Subtitles.Drawings;
using SkiaSharp;

namespace MovieSharp.Sources.Subtitles;
internal class DrawingSubtitleSource : SubtitleSourceBase
{
    public DrawingSubtitleSource((int width, int height) renderBound, double framerate) : base(renderBound, framerate)
    {

    }

    public override float Measure(string text, FontDefinition font, out (float x, float y, float w, float h) bound)
    {
        throw new NotImplementedException();
    }

    protected override void DrawTextBox(DrawingTextBox text)
    {
        throw new NotImplementedException();
    }
}
