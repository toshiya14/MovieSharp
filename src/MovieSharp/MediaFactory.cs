using MovieSharp.Composers;
using MovieSharp.Objects;
using MovieSharp.Sources.Audios;
using MovieSharp.Sources.Videos;

namespace MovieSharp;

public class MediaFactory
{
    public IVideoSource LoadVideo(string filepath, (int?, int?)? targetResolution = null, PixelFormat? pixfmt = null, string resizeAlgo = "bicubic", string ffmpeg = "ffmpeg")
    {
        var vid = new FFVideoFileSource(filepath, targetResolution)
        {
            PixelFormat = pixfmt ?? PixelFormat.RGBA32,
            ResizeAlgo = resizeAlgo,
            FFMpegPath = ffmpeg
        };
        vid.Init();
        return vid;
    }

    public IVideoSource MakeDummyVideo(RGBAColor? background, (int, int) size, double duration, PixelFormat? pixfmt = null, double frameRate = 60)
    {
        var vid = new DummyVideoSource(background, pixfmt ?? PixelFormat.RGBA32, size, frameRate, duration);
        return vid;
    }

    public ISubtitleSource CreateSubtitle(int width, int height)
    {
        return new SkiaSubtitleSource((width, height), 60, PixelFormat.RGBA32);
    }

    public IAudioSource LoadAudio(string filepath)
    {
        var aud = new NAudioFileSource(filepath);
        return aud;
    }

    public ICompose NewCompose(int width, int height, double framerate, double duration = -1)
    {
        var com = new Compose(width, height, duration, framerate);
        return com;
    }
}
