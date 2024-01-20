using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronSoftware.Drawing;
using MovieSharp.Objects.Subtitles;

namespace MovieSharp.Fonts;
public interface IFontManager<TFont>
{
    void Add(string path);
    TFont CreateFont(Font font);
    IEnumerable<string> Names();
}
