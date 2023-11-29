using MovieSharp.Objects;
using SkiaSharp;

namespace MovieSharp.Skia;

public static class DrawVideoFrameExtensions
{

    public static unsafe void DrawVideoFrame(this SKCanvas cvs, SKPaint? paint, IVideoSource vid, double t, SKPoint pos)
    {
        var (width, height) = vid.Size;
        var frame = vid.MakeFrameByTime(t);
        var wantedLength = width * height * 4;
        if (frame is not null)
        {
            if (frame.Value.Length != wantedLength || vid.PixelFormat != PixelFormat.RGBA32)
            {
                throw new ArgumentException($"MakeFrameByTime returns {frame.Value.Length} bytes but {wantedLength} wanted. Maybe the pixel format is not correct. Pixel format in VideoSource is {vid.PixelFormat}, wanted: rgba/rgba8888/rgba32.");
            }
            var mh = frame.Value.Pin();
            var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            var bitmap = new SKBitmap(info);
            bitmap.InstallPixels(info, (IntPtr)mh.Pointer, bitmap.RowBytes, delegate { mh.Dispose(); }, cvs);
            if (paint is null)
            {
                cvs.DrawBitmap(bitmap, pos);
            }
            else
            {
                cvs.DrawBitmap(bitmap, pos, paint);
            }
        }
    }

    public static void DrawVideoFrame(this SKCanvas cvs, SKPaint? paint, IVideoSource vid, double t)
    {
        cvs.DrawVideoFrame(paint, vid, t, new SKPoint(0, 0));
    }
}
