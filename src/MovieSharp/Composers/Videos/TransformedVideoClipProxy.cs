using MovieSharp.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp.Composers.Videos;

internal class TransformedVideoClipProxy : IVideoClip, ITransformableVideo
{
    private readonly IVideoClip baseclip;
    private readonly List<(string, float, float?)> transforms = new();

    public Coordinate Size {
        get { 
            var (wi, hi) = baseclip.Size;
            float w = wi, h = hi;
            foreach (var t in transforms) {
                var (proc, p1, p2) = t;
                switch (proc) {
                    case "translate":
                        w += p1;
                        h += p2!.Value;
                        break;

                    case "scale":
                        w *= p1;
                        h *= p2!.Value;
                        break;
                }
            }
            return new((int)w, (int)h);
        }
    }

    public double Duration => this.baseclip.Duration;

    public TransformedVideoClipProxy(IVideoClip baseclip)
    {
        this.baseclip = baseclip;
    }

    public void AddTranslate(float x, float y) {
        this.transforms.Add(("translate", x, y));
    }

    public void AddScale(float factor) {
        this.transforms.Add(("scale", factor, null));
    }

    public void AddScale(float x, float y) {
        this.transforms.Add(("scale", x, y));
    }

    public void Draw(SKCanvas canvas, double time)
    {
        canvas.Save();

        // Build transform
        foreach (var transform in this.transforms) {
            var (proc, param1, param2) = transform;
            switch (proc) {
                case "translate":
                    canvas.Translate(param1, param2!.Value);
                    break;

                case "scale":
                    var scaleX = param1;
                    var scaleY = param2 ?? scaleX;
                    canvas.Scale(scaleX, scaleY);
                    break;
            }
        }
        this.baseclip.Draw(canvas, time);

        canvas.Restore();
    }

    public void Dispose() {
        this.baseclip.Dispose();
        GC.SuppressFinalize(this);
    }
}
