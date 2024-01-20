using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace MovieSharp.Objects.Subtitles;
internal class FontCache
{
    private Dictionary<string, string> fontPaths = new();
    private Dictionary<string, SKTypeface> fontCache = new();

    public void Add(string name, string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Font file not found: " + path);
        }

        this.fontPaths[name] = path;
        this.fontCache[name] = SKTypeface.FromFile(path);
    }

    public SKTypeface Get(string name)
    {
        if (this.fontCache.ContainsKey(name))
        {
            return this.fontCache[name];
        }
        else
        {
            throw new InvalidOperationException("Please add font into SixLabordFontManager first, then Get it.");
        }
    }
}
