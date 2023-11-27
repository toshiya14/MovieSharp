
using System.Diagnostics;
using MovieSharp.Composers.Timelines;
using MovieSharp.Exceptions;
using MovieSharp.Objects;
using MovieSharp.Objects.EncodingParameters;
using MovieSharp.Targets.Videos;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NLog;
using SkiaSharp;

namespace MovieSharp.Composers;

public class OnFrameWrittenEventArgs
{
    public long Total { get; set; }
    public long Finished { get; set; }
    public int EllapsedTime { get; set; }
}

internal class Compose : ICompose
{
#if DEBUG
    private ILogger log = LogManager.GetCurrentClassLogger();
#endif

    private List<ComposeVideoTrack> videos = new();

    private List<ComposeAudioTrack> audios = new();

    private double duration;

    private bool isAudioComposed = false;

    public Coordinate Size { get; }

    public double Duration => duration < 0 ? SubClipsDuration : duration;

    public event EventHandler<OnFrameWrittenEventArgs>? OnFrameWritten;
    public event EventHandler<FFProgressData>? OnFrameEncoded;

    public double SubClipsDuration
    {
        get
        {
            var vd = this.videos.Select(x => x.Time + x.Clip.Duration).Max();
            var ad = this.audios.Select(x => x.Time + x.Clip.Duration).Max();
            return Math.Max(vd, ad);
        }
    }

    public double FrameRate { get; set; }
    public TimeRange? RenderRange { get; set; }
    public string OutputFile { get; set; } = string.Empty;
    public string? TempAudioFile { get; set; }
    public int Channels { get; }
    public int SampleRate { get; }

    public Compose(int width, int height, double duration = -1, double frameRate = 60, int channels = 2, int samplerate = 44100)
    {
        this.Size = new Coordinate(width, height);
        this.duration = duration;
        this.FrameRate = frameRate;
        this.Channels = channels;
        this.SampleRate = samplerate;
    }


    public void PutVideo(double time, IVideoClip clip)
    {
        this.videos.Add(new(time, clip));
    }

    public void PutAudio(double time, IAudioClip clip)
    {
        var _clip = clip;
        if (this.Channels != clip.Channels)
        {
            _clip = _clip.ChangeChannels(this.Channels);
        }
        if (this.SampleRate != clip.SampleRate)
        {
            _clip = _clip.Resample(this.SampleRate);
        }
        this.audios.Add(new(time, _clip));
    }

    public void Draw(SKCanvas canvas, SKPaint? paint, double time)
    {
        var (w, h) = this.Size;
        using var bmp = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var cvs = new SKCanvas(bmp);
        foreach (var layout in videos.Where(x => x.Time < time))
        {
            var offsetTime = time - layout.Time;
            layout.Clip.Draw(cvs, paint, offsetTime);
        }
        cvs.Flush();
        canvas.DrawBitmap(bmp, 0, 0);
    }

    public ISampleProvider GetSampler()
    {
        var offseted = this.audios.Select(x =>
        {
            var offset = new OffsetSampleProvider(x.Clip.GetSampler())
            {
                DelayBy = TimeSpan.FromSeconds(x.Time)
            };
            return offset;
        });
        var mixer = new MixingSampleProvider(offseted);
        return mixer;
    }

    public void Dispose()
    {
        foreach (var v in videos)
        {
            v.Clip.Dispose();
        }
        foreach (var a in audios)
        {
            a.Clip.Dispose();
        }
        GC.SuppressFinalize(this);
    }

    public TimeRange DetectMaxRange() {
        var maxAudioDuration = this.audios.Select(x => x.Time + x.Clip.Duration).Max();
        var maxVideoDuration = this.videos.Select(x => x.Time + x.Clip.Duration).Max();
        return new TimeRange(0, Math.Min(maxAudioDuration, maxVideoDuration));
    }

    public void ComposeVideo(FFVideoParams? p = null)
    {
        if (string.IsNullOrWhiteSpace(this.OutputFile))
        {
            throw new ArgumentException("`OutputFile` should be set before call Compose or ComposeVideo.");
        }

        var param = p ?? new FFVideoParams();
        param.FrameRate ??= (float)this.FrameRate;
        param.Size ??= this.Size;
        if (isAudioComposed)
        {
            param.WithCopyAudio = this.TempAudioFile;
        }

        double start, end;
        if (this.RenderRange is not null)
        {
            start = this.RenderRange.Value.Left;
            end = this.RenderRange.Value.Right;
        }
        else
        {
            start = 0;
            end = this.Duration;
        }

        var maxVideoDuration = this.videos.Select(x => x.Time + x.Clip.Duration).Max();
        if (end > maxVideoDuration)
        {
            throw new MovieSharpException(MovieSharpErrorType.RenderRangeOverflow, $"Max video duration in this compose can not satisfied the end of rendering. (max: {maxVideoDuration}, end: {end})");
        }
        var startFrame = (long)(start * this.FrameRate);
        var endFrame = (long)(end * this.FrameRate);

        using var writer = new FFVideoFileTarget(param, this.OutputFile, param.FFMPEGBinary);
        using var bmp = new SKBitmap(param.Size.X, param.Size.Y, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        var step = 1.0 / this.FrameRate;

        writer.Init();
        writer.OnProgress += (sender, e) =>
        {
            this.OnFrameEncoded?.Invoke(sender, e);
        };
        for (var i = startFrame; i <= endFrame; i++)
        {
            var ellapsed = 0;
#if DEBUG
            var sw = Stopwatch.StartNew();
#endif
            var time = i * step;
            using var cvs = new SKCanvas(bmp);
            cvs.Clear();
            this.Draw(cvs, null, time);
            cvs.Flush();
            writer.WriteFrame(bmp.Bytes.AsMemory());

#if DEBUG
            sw.Stop();
            ellapsed = (int)sw.ElapsedMilliseconds;
#endif
            this.OnFrameWritten?.Invoke(this, new OnFrameWrittenEventArgs { EllapsedTime = ellapsed, Finished = i, Total = endFrame });
        }
#if DEBUG
        log.Info("Finished. Inner Errors:");
        log.Warn(writer.GetErrors());
#endif
    }

    public void ComposeAudio(NAudioParams? p = null)
    {
        var param = p ?? new NAudioParams();

        var sampler = param.Resample is null ?
            this.GetSampler() :
            new MediaFoundationResampler(this.GetSampler().ToWaveProvider(), param.Resample.Value).ToSampleProvider();


        double start, end;
        if (this.RenderRange is not null)
        {
            start = this.RenderRange.Value.Left;
            end = this.RenderRange.Value.Right;
        }
        else
        {
            start = 0;
            end = this.Duration;
        }

        var maxAudioLength = this.audios.Select(x => x.Time + x.Clip.Duration).Max();
        if (end > maxAudioLength) {

            throw new MovieSharpException(MovieSharpErrorType.RenderRangeOverflow, $"Max audio duration in this compose can not satisfied the end of rendering. (max: {maxAudioLength}, end: {end})");
        }

        var slice = new OffsetSampleProvider(sampler) { 
            SkipOver = TimeSpan.FromSeconds(start),
            Take = TimeSpan.FromSeconds(end - start)
        };

        var wave = slice.ToWaveProvider();

        var outputPath = this.TempAudioFile ?? Path.GetTempFileName();
        switch (param.Codec.ToLower())
        {
            default:
                throw new ArgumentException($"Unknown audio codec: {param.Codec}.");

            case "aac":
                outputPath += ".aac";
                MediaFoundationEncoder.EncodeToAac(wave, outputPath, param.Bitrate);
                break;

            case "mp3":
                outputPath += ".mp3";
                MediaFoundationEncoder.EncodeToMp3(wave, outputPath, param.Bitrate);
                break;

            case "wav":
                outputPath += ".wav";
                WaveFileWriter.CreateWaveFile16(outputPath, wave.ToSampleProvider());
                break;
        }

        this.TempAudioFile = outputPath;
        isAudioComposed = true;
    }

    void ICompose.Compose(FFVideoParams? vp, NAudioParams? ap)
    {
        this.ComposeAudio(ap);
        this.ComposeVideo(vp);
    }
}
