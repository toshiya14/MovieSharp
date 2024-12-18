using MovieSharp.Objects;
using MovieSharp.Sources.Videos;
using MovieSharp.Composers.Audios;
using MovieSharp.Composers.Videos;
using MovieSharp.Fonts;
using MovieSharp.Sources.Audios;
using MovieSharp.Sources.Subtitles;
using SkiaSharp;
using MovieSharp.Composers;

namespace MovieSharp;

public enum SubtitleBackend
{
    Skia,
    GDI
}

public static class MediaFactory
{
    public static string FFMPEGFolder { get; set; } = string.Empty;
    public static IFontManager<SKFont> FontManager { get; } = new SkiaFontManager();

    internal static string GetFFBinPath(string bin) {
        var ffpath = FFMPEGFolder;
        if (string.IsNullOrWhiteSpace(ffpath))
        {
            ffpath = bin;
        }
        else
        {
            ffpath = Path.Join(ffpath, bin);
        }
        return ffpath;
    }

    public static IVideoSource LoadVideo(string filepath, VideoFileSourceFitPolicy fitPolicy, (int?, int?)? resolution = null, string resizeAlgo = "lanczos", double speed = 1.0)
    {
        var vid = new FFVideoFileSource(filepath, fitPolicy, resolution, speed)
        {
            ResizeAlgo = resizeAlgo,
        };
        return vid;
    }

    public static IVideoSource LoadImage(string filepath)
    {
        var vid = new SkiaSequenceSource(filepath);
        return vid;
    }

    public static IVideoSource MakeDummyVideo(RGBAColor? background, (int, int) size, double duration, PixelFormat? pixfmt = null, double frameRate = 60)
    {
        var vid = new DummyVideoSource(background, pixfmt ?? PixelFormat.RGBA32, size, frameRate, duration);
        return vid;
    }

    public static ISubtitleSource CreateSubtitle(int width, int height, SubtitleBackend backend = SubtitleBackend.Skia)
    {
        return backend switch
        {
            SubtitleBackend.Skia => new SkiaSubtitleSource((width, height), 60, FontManager),
            SubtitleBackend.GDI => throw new NotImplementedException(),
            //return new DrawingSubtitleSource((width, height), 60);
            _ => throw new NotSupportedException($"Not supported subtitle backend: {backend}"),
        };
    }

    public static IAudioSource LoadAudio(string filepath)
    {
        return new NAudioFileSource(filepath);
    }

    public static IAudioSource LoadAudio(Stream stream)
    {
        return new NAudioStreamSource(stream);
    }

    public static ICompose NewCompose(int width, int height, double framerate, double duration = -1)
    {
        return new Compose(width, height, duration, framerate);
    }

    public static IAudioClip ZeroAudioClip(int channels, int sampleRate)
    {
        return new ZeroAudioClip(channels, sampleRate);
    }

    public static IVideoClip ZeroVideoClip(int width, int height)
    {
        return new ZeroVideoClip(width, height);
    }
}
