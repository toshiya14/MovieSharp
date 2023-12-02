using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace MovieSharp.Skia.Surfaces;
internal interface ISurfaceProxy : IDisposable
{
    SKImageInfo ImageInfo { get; }
    SKImage Snapshot();
    SKCanvas Canvas { get; }
}
