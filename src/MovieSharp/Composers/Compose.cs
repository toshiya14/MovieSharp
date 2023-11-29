﻿
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

    public double Duration => this.duration < 0 ? this.SubClipsDuration : this.duration;

    public event EventHandler<OnFrameWrittenEventArgs>? OnFrameWritten;
    public event EventHandler<FFProgressData>? OnFrameEncoded;

    public double SubClipsDuration
    {
        get
        {
            var vd = this.videos.Any() ? this.videos.Select(x => x.Time + x.Clip.Duration).Max() : 0;
            var ad = this.audios.Any() ? this.audios.Select(x => x.Time + x.Clip.Duration).Max() : 0;
            return Math.Max(vd, ad);
        }
    }

    public double FrameRate { get; set; }
    public TimeRange? RenderRange { get; set; }
    public string OutputFile { get; set; } = string.Empty;
    public string? TempAudioFile { get; set; }
    public int Channels { get; }
    public int SampleRate { get; }

    /// <summary>
    /// Create a compose.
    /// </summary>
    /// <param name="width">The width of each frame.</param>
    /// <param name="height">The height of each frame.</param>
    /// <param name="duration">The duration of this compose, if set to negative number, would detect the durations of all videos and audios tracks, then use the max duration.</param>
    /// <param name="frameRate">The frame rate for the video.</param>
    /// <param name="channels">The audio channels.</param>
    /// <param name="samplerate">The audio sample rate.</param>
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
        if (time > this.Duration)
        {
            //throw new MovieSharpException(MovieSharpErrorType.DrawingTimeGreaterThanDuration, $"Current drawing time: {time}, duration for this compose: {this.Duration}");
            return;
        }
        if (this.videos.Count > 0)
        {
            var (w, h) = this.Size;
            using var bmp = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            using var cvs = new SKCanvas(bmp);
            foreach (var layout in this.videos.Where(x => x.Time < time))
            {
                var offsetTime = time - layout.Time;
                layout.Clip.Draw(cvs, paint, offsetTime);
            }
            cvs.Flush();
            canvas.DrawBitmap(bmp, 0, 0);
        }
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

        var empty = new NAudio.Wave.SilenceProvider(new WaveFormat(this.SampleRate, this.Channels));

        return offseted.Any() ? new MixingSampleProvider(offseted) : empty.ToSampleProvider();
    }

    public void Dispose()
    {
        foreach (var v in this.videos)
        {
            v.Clip.Dispose();
        }
        foreach (var a in this.audios)
        {
            a.Clip.Dispose();
        }
        GC.SuppressFinalize(this);
    }

    public void UseMaxRenderRange()
    {
        this.RenderRange = new TimeRange(0, this.Duration);
    }

    public void ComposeVideo(FFVideoParams? p = null)
    {
        if (string.IsNullOrWhiteSpace(this.OutputFile))
        {
            throw new ArgumentException("`OutputFile` should be set before call Compose or ComposeVideo.");
        }
        if (this.RenderRange is null)
        {
            throw new MovieSharpException(MovieSharpErrorType.RenderRangeNotSet, "Compose.RenderRange should be set before calling any Compose() functions. Or use Compose.UseMaxRenderRange() to auto detect the range.");
        }
        if (!this.videos.Any())
        {
            throw new MovieSharpException(MovieSharpErrorType.ComposeNoClip, "ComposeVideo() called, but there is no video clip inside this compose.");
        }

        var param = p ?? new FFVideoParams();
        param.FrameRate ??= (float)this.FrameRate;
        param.Size ??= this.Size;
        if (this.isAudioComposed)
        {
            param.WithCopyAudio = this.TempAudioFile;
        }

        var start = this.RenderRange.Value.Left;
        var end = this.RenderRange.Value.Right;

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
            cvs.Clear(param.TransparentColor.ToSKColor());
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
        this.log.Info("Finished. Inner Errors:");
        this.log.Warn(writer.GetErrors());
#endif
    }

    public void ComposeAudio(NAudioParams? p = null)
    {
        if (this.RenderRange is null)
        {
            throw new MovieSharpException(MovieSharpErrorType.RenderRangeNotSet, "Compose.RenderRange should be set before calling any Compose() functions. Or use Compose.UseMaxRenderRange() to auto detect the range.");
        }

        var param = p ?? new NAudioParams();

        var sampler = param.Resample is null ?
            this.GetSampler() :
            new MediaFoundationResampler(this.GetSampler().ToWaveProvider(), param.Resample.Value).ToSampleProvider();

        var start = this.RenderRange.Value.Left;
        var end = this.RenderRange.Value.Right;

        // cut and keep the length same with compose itself.
        var slice = new OffsetSampleProvider(sampler)
        {
            SkipOver = TimeSpan.FromSeconds(start),
            Take = TimeSpan.FromSeconds(end - start)
        };
        var empty = new SilenceProvider(slice.WaveFormat).ToSampleProvider().Take(TimeSpan.FromSeconds(end - start));
        var wave = new MixingSampleProvider(new[] { slice, empty }).ToWaveProvider();

        var outputPath = this.TempAudioFile ?? Path.GetTempFileName();

        this.log.Debug($"Compose audio: {start} - {end}, path: {outputPath}");

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
        this.isAudioComposed = true;
    }

    void ICompose.Compose(FFVideoParams? vp, NAudioParams? ap)
    {
        if (this.RenderRange is null)
        {
            throw new MovieSharpException(MovieSharpErrorType.RenderRangeNotSet, "Compose.RenderRange should be set before calling any Compose() functions. Or use Compose.UseMaxRenderRange() to auto detect the range.");
        }
        this.ComposeAudio(ap);
        this.ComposeVideo(vp);
    }
}
