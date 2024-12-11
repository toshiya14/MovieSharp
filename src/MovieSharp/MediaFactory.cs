using IronSoftware.Drawing;
using MovieSharp.Composers;
using MovieSharp.Composers.Audios;
using MovieSharp.Composers.Videos;
using MovieSharp.Fonts;
using MovieSharp.Objects;
using MovieSharp.Objects.Subtitles;
using MovieSharp.Sources.Audios;
using MovieSharp.Sources.Subtitles;
using MovieSharp.Sources.Videos;
using SkiaSharp;

namespace MovieSharp;

public enum SubtitleBackend
{
    Skia,
    GDI
}

public class MediaFactory
{
    public string FFMPEGBinary => string.IsNullOrEmpty(this.FFMPEGFolder) ? "ffmpeg" : Path.Combine(this.FFMPEGFolder, "ffmpeg");
    public string FFPROBEBinary => string.IsNullOrEmpty(this.FFMPEGFolder) ? "ffprobe" : Path.Combine(this.FFMPEGFolder, "ffprobe");
    public string FFMPEGFolder { get; set; } = string.Empty;
    //private SixLabordFontManager SharedFontCache { get; } = new SixLabordFontManager();
    //private Dictionary<string, Font> SixLabordFontManager { get; } = new();
    public IFontManager<SKFont> FontManager { get; } = new SkiaFontManager();

    public IVideoSource LoadVideo(string filepath, (int?, int?)? targetResolution = null, string resizeAlgo = "bicubic")
    {
        var vid = new FFVideoFileSource(filepath, targetResolution)
        {
            ResizeAlgo = resizeAlgo,
            FFMpegPath = this.FFMPEGBinary,
            FFMpegBinFolder = this.FFMPEGFolder,
        };
        return vid;
    }

    public IVideoSource LoadImage(string filepath)
    {
        var vid = new SkiaSequenceSource(filepath);
        return vid;
    }

    public IVideoSource MakeDummyVideo(RGBAColor? background, (int, int) size, double duration, PixelFormat? pixfmt = null, double frameRate = 60)
    {
        var vid = new DummyVideoSource(background, pixfmt ?? PixelFormat.RGBA32, size, frameRate, duration);
        return vid;
    }

    public ISubtitleSource CreateSubtitle(int width, int height, SubtitleBackend backend = SubtitleBackend.Skia)
    {
        return backend switch
        {
            SubtitleBackend.Skia => new SkiaSubtitleSource((width, height), 60, this.FontManager),
            SubtitleBackend.GDI => throw new NotImplementedException(),
            //return new DrawingSubtitleSource((width, height), 60);
            _ => throw new NotSupportedException($"Not supported subtitle backend: {backend}"),
        };
    }

    public IAudioSource LoadAudio(string filepath)
    {
        return new NAudioFileSource(filepath);
    }

    public IAudioSource LoadAudio(Stream stream)
    {
        return new NAudioStreamSource(stream);
    }

    public ICompose NewCompose(int width, int height, double framerate, double duration = -1)
    {
        return new Compose(width, height, duration, framerate, ffmpegBin: this.FFMPEGBinary);
    }

    public IAudioClip ZeroAudioClip(int channels, int sampleRate)
    {
        return new ZeroAudioClip(channels, sampleRate);
    }

    public IVideoClip ZeroVideoClip(int width, int height)
    {
        return new ZeroVideoClip(width, height);
    }
}
