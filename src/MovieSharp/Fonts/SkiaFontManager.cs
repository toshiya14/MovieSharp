using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MovieSharp.Exceptions;
using NLog;
using SkiaSharp;

namespace MovieSharp.Fonts;
internal class SkiaFontManager : IFontManager<SKFont>
{
    public List<SKTypeface> typefaces = new();
    public Dictionary<string, string> namemap = new();
    private readonly ILogger log = LogManager.GetLogger(nameof(SkiaFontManager));

    public void Add(string path)
    {
        var face = SKTypeface.FromFile(path);
        if (face == null)
        {
            this.log.Warn($"{path} is not a valid font file, skip it.");
            return;
        }
        this.typefaces.Add(face);
    }

    public SKFont CreateFont(IronSoftware.Drawing.Font font)
    {
        var isBold = font.Bold;
        var isItalic = font.Italic;
        var familyName = font.FamilyName;

        var fonts = from f in typefaces
                    where f.FamilyName == familyName
                    select f;
        if (fonts.Any())
        {
            SKTypeface? boldface = null, italicface = null, bolditaface = null, normalface = null, useface = null;
            foreach (var f in fonts)
            {
                if (f.IsBold && f.IsItalic)
                {
                    bolditaface = f;
                }
                else if (f.IsBold)
                {
                    boldface = f;
                }
                else if (f.IsItalic)
                {
                    italicface = f;
                }
                else if (!f.IsBold && !f.IsItalic)
                {
                    normalface = f;
                }
            }

            if (normalface == null)
            {
                normalface = fonts.First();
            }


            if (isBold && isItalic)
            {
                useface = bolditaface ?? boldface ?? italicface ?? normalface;
            }
            else if (isBold)
            {
                useface = boldface ?? normalface;
            }
            else if (isItalic)
            {
                useface = italicface ?? normalface;
            }
            else
            {
                useface = normalface;
            }

            return new SKFont(useface, font.Size);
        }
        else
        {
            throw new MovieSharpException(MovieSharpErrorType.FontFileNotFound, $"Font with family name: {familyName} could not be found.");
        }
    }

    public IEnumerable<string> Names()
    {
        return this.typefaces.Select(x => x.FamilyName).Distinct();
    }

    public bool IsAvailableFont(string name)
    {
        return this.typefaces.Any(x => x.FamilyName == name);
    }
}
