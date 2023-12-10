using MovieSharp.Composers;
using MovieSharp.Composers.Audios;
using MovieSharp.Composers.Videos;
using MovieSharp.Objects;
using MovieSharp.Objects.Subtitles;
using MovieSharp.Sources.Audios;
using MovieSharp.Sources.Videos;

namespace MovieSharp;

public class MediaFactory
{
    public string FFMPEGBinary => string.IsNullOrEmpty(this.FFMPEGFolder) ? "ffmpeg" : Path.Combine(this.FFMPEGFolder, "ffmpeg");
    public string FFPROBEBinary => string.IsNullOrEmpty(this.FFMPEGFolder) ? "ffprobe" : Path.Combine(this.FFMPEGFolder, "ffprobe");
    public string FFMPEGFolder { get; set; } = string.Empty;
    private FontCache SharedFontCache { get; } = new FontCache();


    public IVideoSource LoadVideo(string filepath, (int?, int?)? targetResolution = null, string resizeAlgo = "bicubic")
    {
        var vid = new FFVideoFileSource(filepath, targetResolution)
        {
            ResizeAlgo = resizeAlgo,
            FFMpegPath = this.FFMPEGBinary,
            FFMpegBinFolder = this.FFMPEGFolder,
        };
        vid.Init();
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

    public ISubtitleSource CreateSubtitle(int width, int height)
    {
        return new SkiaSubtitleSource((width, height), 60, PixelFormat.RGBA32, this.SharedFontCache);
    }

    public void AddLocalFont(string name, string path)
    {
        this.SharedFontCache.Add(name, path);
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
