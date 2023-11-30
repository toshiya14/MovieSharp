using MovieSharp.Composers;
using MovieSharp.Composers.Audios;
using MovieSharp.Composers.Videos;
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
        return new NAudioFileSource(filepath);
    }

    public IAudioSource LoadAudio(Stream stream)
    {
        return new NAudioStreamSource(stream);
    }

    public ICompose NewCompose(int width, int height, double framerate, double duration = -1, CancellationTokenSource? cts = null)
    {
        return new Compose(width, height, duration, framerate, cts: cts);
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
