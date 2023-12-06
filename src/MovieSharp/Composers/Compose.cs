
using System.Diagnostics;
using MovieSharp.Composers.Timelines;
using MovieSharp.Debugs.Benchmarks;
using MovieSharp.Exceptions;
using MovieSharp.Objects;
using MovieSharp.Objects.EncodingParameters;
using MovieSharp.Skia.Surfaces;
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
    private ILogger log = LogManager.GetCurrentClassLogger();
    private List<ComposeVideoTrack> videos = new();
    private List<ComposeAudioTrack> audios = new();
    private double duration;
    private bool isAudioComposed = false;
    private readonly CancellationTokenSource cts;
    private ISurfaceProxy surface;

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
    public string FFMPEGBinary { get; }

    /// <summary>
    /// Create a compose.
    /// </summary>
    /// <param name="width">The width of each frame.</param>
    /// <param name="height">The height of each frame.</param>
    /// <param name="duration">The duration of this compose, if set to negative number, would detect the durations of all videos and audios tracks, then use the max duration.</param>
    /// <param name="frameRate">The frame rate for the video.</param>
    /// <param name="channels">The audio channels.</param>
    /// <param name="samplerate">The audio sample rate.</param>
    public Compose(int width, int height, double duration = -1, double frameRate = 60, int channels = 2, int samplerate = 44100, string ffmpegBin = "ffmpeg")
    {
        this.Size = new Coordinate(width, height);
        this.duration = duration;
        this.FrameRate = frameRate;
        this.Channels = channels;
        this.SampleRate = samplerate;
        this.FFMPEGBinary = ffmpegBin;
        this.cts = new CancellationTokenSource();
        this.surface = new RasterSurface(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
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
            this.surface.Canvas.Clear();
            foreach (var layout in this.videos.Where(x => x.Time < time))
            {
                var offsetTime = time - layout.Time;
                layout.Clip.Draw(this.surface.Canvas, paint, offsetTime);
            }
            this.surface.Canvas.Flush();

            using var img = this.surface.Snapshot();
            canvas.DrawImage(img, 0, 0);
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
        this.surface.Dispose();
        GC.SuppressFinalize(this);
    }

    public void UseMaxRenderRange()
    {
        this.RenderRange = new TimeRange(0, this.Duration);
    }

    public void Cancel()
    {
        this.cts.Cancel();
    }

    private async Task ComposeVideo(FFVideoParams? p = null)
    {
        var pm = PerformanceMeasurer.GetCurrentClassMeasurer();

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

        using var writer = new FFVideoFileTarget(param, this.OutputFile, this.FFMPEGBinary);

        var step = 1.0 / this.FrameRate;

        var surface = new RasterSurface(new SKImageInfo(param.Size.X, param.Size.Y, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        writer.Init();
        writer.OnProgress += (sender, e) =>
        {
            this.OnFrameEncoded?.Invoke(sender, e);
        };
        for (var i = startFrame; i <= endFrame; i++)
        {
            if (this.cts.IsCancellationRequested)
            {
#if DEBUG
                this.log.Info("Composing action has been cancelled.");
#endif
                break;
            }
            await Task.Run(() =>
            {
                using var _ = pm.UseMeasurer("compose-frame");
                var ellapsed = 0;

                var time = i * step;

                surface.Canvas.Clear(param.TransparentColor.ToSKColor());
                this.Draw(surface.Canvas, null, time);
                surface.Canvas.Flush();

                using var img = surface.Snapshot();
                using var pixmap = img.PeekPixels();
                pixmap.GetPixelSpan();

                writer.WriteFrame(pixmap.GetPixelSpan());

                this.OnFrameWritten?.Invoke(this, new OnFrameWrittenEventArgs { EllapsedTime = ellapsed, Finished = i, Total = endFrame });
            }, this.cts.Token);
        }
#if DEBUG
        var error = writer.GetErrors();
        if (string.IsNullOrWhiteSpace(error))
        {
            this.log.Info("Finished normally.");
        }
        else
        {
            this.log.Info("Finished. Inner Errors:\n");
            this.log.Warn(error);
        }
#endif
    }

    private async Task ComposeAudio(NAudioParams? p = null)
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

        await Task.Run(() =>
        {
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
        }, this.cts.Token);


        this.TempAudioFile = outputPath;
        this.isAudioComposed = true;
    }

    void ICompose.Compose(FFVideoParams? vp, NAudioParams? ap)
    {
        this.cts.TryReset();
        if (this.RenderRange is null)
        {
            throw new MovieSharpException(MovieSharpErrorType.RenderRangeNotSet, "Compose.RenderRange should be set before calling any Compose() functions. Or use Compose.UseMaxRenderRange() to auto detect the range.");
        }
        this.ComposeAudio(ap).Wait();
        this.ComposeVideo(vp).Wait();
    }
}
