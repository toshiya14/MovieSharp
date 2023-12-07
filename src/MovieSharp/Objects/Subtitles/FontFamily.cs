using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace MovieSharp.Objects.Subtitles;

public enum FontSource
{
    System,
    File
}

public struct FontFamily
{
    public FontSource Source { get; set; }
    public string Name { get; set; }

    public FontFamily(FontSource source, string name)
    {
        this.Source = source;
        this.Name = name;
    }
}
