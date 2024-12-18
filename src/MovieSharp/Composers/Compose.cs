
using System.Buffers;
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
    public long Finished { get; set; }
    public long ElapsedTime { get; set; }
}

internal class Compose : ICompose
{
    private ILogger log = LogManager.GetCurrentClassLogger();
    private List<ComposeVideoTrack> videos = new();
    private List<ComposeAudioTrack> audios = new();
    private double duration;
    private bool isAudioComposed = false;
    private readonly CancellationTokenSource cts;

    public Coordinate Size { get; }
    public SKImageInfo ImageInfo => new(this.Size.X, this.Size.Y, SKColorType.Rgba8888, SKAlphaType.Unpremul);

    public double Duration => this.duration < 0 ? this.SubClipsDuration : this.duration;

    public event EventHandler<OnFrameWrittenEventArgs>? OnFrameWritten;
    public event EventHandler<FFProgressData>? OnFrameEncoded;
    public event EventHandler? OnCancelled;
    public event EventHandler? OnCompleted;

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
    public bool OnlyEncodeAudio { get; set; } = false;
    public NAudioParams AudioParams { get; set; } = new();
    public FFVideoParams VideoParams { get; set; } = new();

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
        this.cts = new CancellationTokenSource();
    }

    public void PutVideo(double time, IVideoClip clip, double renderOrder)
    {
        // search insert point
        var i = 0;
        for (; i < this.videos.Count; i++)
        {
            var video = this.videos[i];
            if (video.DrawingOrder > renderOrder)
            {
                break;
            }
        }

        this.videos.Insert(i, new(time, clip, renderOrder));
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

    public void DrawVideosById(SKCanvas canvas, SKPaint? paint, long frameIndex)
    {
        var time = frameIndex * this.FrameRate;
        this.DrawVideos(canvas, paint, time);
    }

    private void DrawVideos(SKCanvas canvas, SKPaint? paint, double time)
    {
        if (time > this.Duration)
        {
            return;
        }

        if (this.videos.Count > 0)
        {
            var (w, h) = this.Size;
            var layoutsForCurrentTime = this.videos.Where(x => x.Time < time);
            if (layoutsForCurrentTime is null || !layoutsForCurrentTime.Any())
            {
                // nothing to draw;
                return;
            }

            foreach (var layout in layoutsForCurrentTime)
            {
                var offsetTime = time - layout.Time;
                layout.Clip.Draw(canvas, paint, offsetTime);
            }
            canvas.Flush();
        }
    }

    public void Draw(SKCanvas canvas, SKPaint? paint, double time)
    {
        //var findex = (long)(time * this.FrameRate);
        //var frame = this.LoadBaseFrame(findex);
        //if (frame is not null && this.BaseFrameProvider is not null)
        //{
        //    using var handle = frame.Value.Pin();
        //    using var img = SKImage.FromPixels(this.BaseFrameProvider.ImageInfo, (nint)handle.Pointer);
        //    canvas.DrawImage(img, new SKPoint(0, 0), paint);
        //}

        this.DrawVideos(canvas, paint, time);
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

    public void Cancel()
    {
        this.cts.Cancel();
    }

    private unsafe void ComposeVideoWithAudio()
    {
        if (string.IsNullOrWhiteSpace(this.OutputFile))
        {
            throw new Exception("LOGIC ERROR: `OutputFile` should be set before call Compose or ComposeVideoWithAudio.");
        }
        if (this.RenderRange is null)
        {
            throw new MovieSharpException(MovieSharpErrorType.RenderRangeNotSet, "Compose.RenderRange should be set before calling any Compose() functions. Or use Compose.UseMaxRenderRange() to auto detect the range.");
        }
        if (this.videos.Count == 0)
        {
            throw new MovieSharpException(MovieSharpErrorType.ComposeNoClip, "ComposeVideoWithAudio() called, but there is no video clip inside this compose.");
        }

        this.log.Info("Prepared to compose video.");

        var start = this.RenderRange.Value.Left;
        var end = this.RenderRange.Value.Right;

        var maxVideoDuration = this.videos.Select(x => x.Time + x.Clip.Duration).Max();
        if (end > maxVideoDuration)
        {
            throw new MovieSharpException(MovieSharpErrorType.RenderRangeOverflow, $"Max video duration in this compose can not satisfied the end of rendering. (max: {maxVideoDuration}, end: {end})");
        }

        var startFrame = (long)(start * this.FrameRate);
        var endFrame = (long)(end * this.FrameRate);
        var step = 1.0 / this.FrameRate;
        var isUserCancelled = false;
        using var surface = new RasterSurface(this.ImageInfo);
        using var writer = this.GetFrameWriter();

        for (var i = startFrame; i <= endFrame; i++)
        {
            if (this.cts.IsCancellationRequested)
            {
                isUserCancelled = true;
                break;
            }
            using var _ = PerformanceMeasurer.UseMeasurer("compose-frame");

            var time = i * step;

            surface.Canvas.Clear(this.VideoParams.TransparentColor.ToSKColor());
            this.Draw(surface.Canvas, null, time);
            surface.Canvas.Flush();

            using var pixmap = surface.Surface.PeekPixels();
            this.WriteNextFrame(writer, i, pixmap.GetPixelSpan());
        }

        var error = writer.GetErrors();
        if (isUserCancelled)
        {
            this.log.Info("Composing action has been cancelled.");
            this.OnCancelled?.Invoke(this, new EventArgs());
        }
        else if (string.IsNullOrWhiteSpace(error))
        {
            this.log.Info("Finished normally.");
            this.OnCompleted?.Invoke(this, new EventArgs());
        }
        else
        {
            this.log.Info("Finished. Inner Errors:\n");
            this.log.Warn(error);
            throw new MovieSharpException(MovieSharpErrorType.SubProcessFailed, error);
        }
    }

    private void DoRelease(double time, IList<ComposeVideoTrack> videos)
    {
        var tobeReleased = new List<ComposeVideoTrack>();

        // mark up.
        foreach (var video in videos)
        {
            var final = video.Time + video.Clip.Duration;
            if (final < time)
            {
                //this.log.Debug($"Release rule matched @ {videos.IndexOf(video)} | {time} | {video.Time} + {video.Clip.Duration}");
                tobeReleased.Add(video);
            }
        }

        // release.
        foreach (var video in tobeReleased)
        {
            video.Clip.Release();
        }
    }

    private void ComposeAudio()
    {
        if (this.RenderRange is null)
        {
            throw new MovieSharpException(MovieSharpErrorType.RenderRangeNotSet, "Compose.RenderRange should be set before calling any Compose() functions. Or use Compose.UseMaxRenderRange() to auto detect the range.");
        }

        var param = this.AudioParams;

        this.log.Info("Prepared to get sampler.");
        var sampler = (param.Resample is null || param.Resample == this.SampleRate) ?
            this.GetSampler() :
            new MediaFoundationResampler(this.GetSampler().ToWaveProvider(), param.Resample.Value).ToSampleProvider();

        var start = this.RenderRange.Value.Left;
        var end = this.RenderRange.Value.Right;

        // cut and keep the length same with compose itself.
        var slice = sampler.Skip(TimeSpan.FromSeconds(start)).Take(TimeSpan.FromSeconds(end - start));
        var wave = slice.ToWaveProvider();

        // 
        string outputPath;
        if (this.OnlyEncodeAudio)
        {
            outputPath = this.OutputFile;
        }
        else
        {
            outputPath = this.TempAudioFile ?? Path.GetTempFileName();
        }

        this.log.Info($"Compose audio: {start} - {end}, path: {outputPath}");

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

    void ICompose.Compose()
    {
        this.cts.TryReset();
        if (this.RenderRange is null)
        {
            this.UseMaxRenderRange();
        }

        this.ComposeAudio();
        if (this.OnlyEncodeAudio)
        {
            this.OnCompleted?.Invoke(this, new EventArgs());
            return;
        }
        this.ComposeVideoWithAudio();

    }

    public void Release()
    {
        throw new NotImplementedException();
    }

    public FFVideoFileTarget GetFrameWriter()
    {
        this.VideoParams.FrameRate ??= (float)this.FrameRate;
        this.VideoParams.Size = this.Size;
        if (this.isAudioComposed)
        {
            this.VideoParams.WithCopyAudio = this.TempAudioFile;
        }
        var writer = new FFVideoFileTarget(this.VideoParams, this.OutputFile, MediaFactory.GetFFBinPath("ffmpeg"));
        writer.Init();
        writer.OnProgress += (sender, e) =>
        {
            this.OnFrameEncoded?.Invoke(sender, e);
        };

        return writer;
    }

    public void WriteNextFrame(FFVideoFileTarget writter, long findex, ReadOnlySpan<byte> buffer)
    {
        var sw = Stopwatch.StartNew();
        var time = findex / this.FrameRate;
        writter.WriteFrame(buffer);
        this.DoRelease(time, this.videos);
        var elapsed = sw.ElapsedMilliseconds;
        this.OnFrameWritten?.Invoke(this, new OnFrameWrittenEventArgs { ElapsedTime = elapsed, Finished = findex });
    }
}
