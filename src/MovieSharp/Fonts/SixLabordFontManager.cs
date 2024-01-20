using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.Fonts;
using SixFont = SixLabors.Fonts.Font;
using SixFontStyle = SixLabors.Fonts.FontStyle;
using IronFont = IronSoftware.Drawing.Font;

namespace MovieSharp.Fonts;

internal class SixLabordFontManager : IFontManager<SixFont>
{
    private readonly FontCollection cols = new();

    public void Add(string path)
    {
        var ext = Path.GetExtension(path).ToLower();
        if (ext is ".otf" or ".ttf" or ".woff" or ".woff2")
        {
            this.cols.Add(path);
        }
        else if (ext is ".ttc")
        {
            this.cols.AddCollection(path);
        }
        else
        {
            // this.log.Warn($"{path} is not a available font file.");
        }
    }

    public SixFont CreateFont(IronFont font)
    {
        var name = font.FamilyName;
        var size = font.Size;
        var style = font.Style;

        if (this.cols.TryGet(name, out var family))
        {
            return family.CreateFont(size, (SixFontStyle)style);
        }
        throw new KeyNotFoundException($"Do not found font family in font manager: {name}");
    }

    public IEnumerable<string> Names()
    {
        foreach (var i in this.cols.Families)
        {
            yield return i.Name;
        }
    }
}
