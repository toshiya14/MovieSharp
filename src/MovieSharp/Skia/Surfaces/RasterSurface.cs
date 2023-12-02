using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace MovieSharp.Skia.Surfaces;
internal class RasterSurface : ISurfaceProxy
{
    public RasterSurface(SKImageInfo info)
    {
        this.ImageInfo = info;
        this.Surface = SKSurface.Create(info);
    }


    public SKCanvas Canvas => this.Surface.Canvas;

    public SKImageInfo ImageInfo { get; }
    public SKSurface Surface { get; }

    public void Dispose()
    {
        this.Surface.Dispose();
    }

    public SKImage Snapshot() => this.Surface.Snapshot();
}
