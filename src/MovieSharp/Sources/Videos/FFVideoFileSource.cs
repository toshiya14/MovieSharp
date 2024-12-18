using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using FFMpegCore;
using MovieSharp.Debugs.Benchmarks;
using MovieSharp.Exceptions;
using MovieSharp.Objects;
using MovieSharp.Tools;
using NLog;
using SkiaSharp;

// This is a port from:
// https://github.com/Zulko/moviepy/blob/9ebabda20a27780101f5c6c832ed398d28377726/moviepy/video/io/ffmpeg_reader.py

namespace MovieSharp.Sources.Videos;

public enum VideoFileSourceFitPolicy
{
    // Add black padding.
    Contain,

    // Crop.
    Cover,

    // Fixed width and crop / padding in Y.
    FixedWidth,

    // Fixed height and crop / padding in Y.
    FixedHeight,
}
internal record FFVideoReInitializedEventArgs(long NewStartFrameIndex);

internal class FFVideoFileSource : IVideoSource, IComposeCanvas
{
    private Process? proc;
    private Stream? stdout;
    private StreamReader? stderr;
    private SKImageInfo imageInfo;
    private Memory<byte>? lastFrame;

    private readonly Logger log = LogManager.GetLogger("MovieSharp.FFVideoFileSource");
    private readonly int bytesPerFrame;
    private readonly double speed;
    private readonly Coordinate targetResolution;
    private readonly Coordinate sourceResolution;

    public VideoFileSourceFitPolicy FitPolicy { get; }
    public string FileName { get; }
    public double FrameRate { get; private set; }
    public double Duration { get; private set; }
    public Coordinate Size => this.targetResolution;
    public int FrameCount { get; private set; }
    public long Position { get; private set; }
    public ReadOnlyMemory<byte>? LastFrame => this.lastFrame;
    public int PixelChannels { get; private set; } = 4;
    public PixelFormat PixelFormat { get; } = PixelFormat.RGBA32;
    public string ResizeAlgo { get; set; } = "bicubic";
    public IMediaAnalysis? Infos { get; private set; }
    internal int BytesPerFrame => this.bytesPerFrame;

    internal event EventHandler<FFVideoReInitializedEventArgs>? OnReInitialized;

    public SKImageInfo ImageInfo => this.imageInfo;


    public FFVideoFileSource(string filename, VideoFileSourceFitPolicy fitPolicy, (int?, int?)? resolution = null, double speed = 1.0)
    {
        var fi = new FileInfo(filename);
        if (!fi.Exists)
        {
            throw new MovieSharpException(MovieSharpErrorType.ResourceNotFound, $"Not found: {fi.FullName}");
        }

        // initialize members;
        this.FileName = filename;
        this.proc = null;

        // load probes.
        FFOptions? ffopt = null;
        if (!string.IsNullOrWhiteSpace(MediaFactory.FFMPEGFolder))
        {
            ffopt = new FFOptions { BinaryFolder = MediaFactory.FFMPEGFolder };
        }

        try
        {
            this.Infos = FFProbe.Analyse(this.FileName, ffopt);
        }
        catch (Exception ex)
        {
            throw new Exception($"ffmpeg executable not found, or failed fetching data from video file: {ex.Message}");
        }

        if (this.Infos.VideoStreams.Count < 1)
        {
            throw new Exception($"{this.FileName} does contain any video stream.");
        }
        var vidstream = this.Infos.VideoStreams[0];

        this.FrameRate = vidstream.FrameRate;
        this.sourceResolution = new Coordinate(vidstream.Width, vidstream.Height);

        // re-calculate target resolution.
        this.targetResolution = Scale(this.sourceResolution, resolution);
        this.bytesPerFrame = this.targetResolution.X * this.targetResolution.Y * this.PixelChannels;
        this.speed = speed;

        this.Duration = vidstream.Duration.TotalSeconds;

        if (this.Duration == 0 && this.Infos.VideoStreams.Count > 0)
        {
            this.Duration = this.Infos.Duration.TotalSeconds;
        }

        if (this.Duration == 0)
        {
            throw new MovieSharpException(MovieSharpErrorType.ResourceLoadingFailed, "Could not determine the duration of the media, maybe it do not contains video stream.");
        }

        this.FrameCount = (int)(vidstream.FrameRate * this.Duration);
        this.imageInfo = new SKImageInfo(this.targetResolution.X, this.targetResolution.Y, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        this.FitPolicy = fitPolicy;

        // TODO: temporary not support rotate.
    }

    /// <summary>
    /// Opens the file, creates the pipe.
    /// Sets `LoadingPosition` to the appropriate value (1 if startTime == 0 because
    /// it pre-reads the first frame).
    /// </summary>
    /// <param name="startTime"></param>
    private void Init(long startIndex = 0)
    {
        var firstTime = this.lastFrame is null;

        // release resources
        this.Close(false);

        using var _ = PerformanceMeasurer.UseMeasurer("init");

        // If there is an running or suspending task, terminate it.
        var arglist = new List<string>();
        var startTime = startIndex / this.FrameRate;

        double offset;
        if (startTime != 0)
        {
            offset = Math.Min(3, startTime);
            arglist.AddRange(new string[] {
                "-hwaccel", "opencl",
                "-ss", (startTime - offset).ToString("f6"),
                "-i", $"\"{this.FileName}\"",
                "-ss", offset.ToString("f6"),
            });
        }
        else
        {
            arglist.AddRange(new string[] {
                "-i",
                $"\"{this.FileName}\"",
            });
        }

        var filters = new List<string>() {
            this.BuildScaleVF(),
        };
        if (Math.Abs(this.speed - 1.0) > 0.001)
        {
            var pts = 1.0 / this.speed;
            filters.Add($"setpts={pts:0.00}*PTS");
        }
        var vf = string.Join(',', filters);

        arglist.AddRange(new string[] {
            "-loglevel", "error",
            "-f", "image2pipe",
            "-vf", vf,
            "-sws_flags", this.ResizeAlgo,
            "-pix_fmt", "rgba",
            "-vcodec", "rawvideo",
            "-"
        });

        var args = string.Join(' ', arglist);
        this.log.Info("Generated args: " + args);

        this.proc = new Process();
        this.proc.StartInfo.FileName = MediaFactory.GetFFBinPath("ffmpeg");
        this.proc.StartInfo.Arguments = args;
        this.proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        this.proc.StartInfo.RedirectStandardOutput = true;
        this.proc.StartInfo.RedirectStandardError = true;
        this.proc.StartInfo.UseShellExecute = false;
        this.proc.StartInfo.CreateNoWindow = true;
        this.proc.Start();

        this.stdout = this.proc.StandardOutput.BaseStream;
        this.stderr = this.proc.StandardError;

        this.Position = startIndex;
        this.lastFrame = (new byte[this.bytesPerFrame]).AsMemory();

        if (!firstTime)
        {
            this.OnReInitialized?.Invoke(this, new(startIndex));
        }
    }

    private void SkipFrames(long n)
    {
        if (n == 0)
        {
            // no need to skip.
            return;
        }

        if (this.lastFrame is null || this.stdout is null)
        {
            this.Init(0);
        }

        if (this.lastFrame is null)
        {
            throw new Exception("LOGIC ERROR: lastFrame still null after Init() called.");
        }

        if (this.stdout is null)
        {
            throw new Exception("LOGIC ERROR: stdout still null after Init() called.");
        }

        for (long i = 0; i < n; i++)
        {
            this.ReadNextFrameFromStdout();
        }
        var prevPosition = this.Position;
        this.Position += n;
        this.log.Debug($"frame skipped for {this.FileName}, n = {n}, from = {prevPosition}, to = {this.Position}.");
    }

    public int GetFrameId(double time) => (int)(this.FrameRate * time + 0.000001);

    private ReadOnlyMemory<byte>? ReadNextFrame()
    {
        if (this.lastFrame is null)
        {
            this.Init(this.Position);
        }

        if (this.lastFrame is null)
        {
            throw new Exception("LOGIC ERROR: lastFrame still null after Init() called.");
        }

        if (this.stdout is null)
        {
            throw new Exception("LOGIC ERROR: stdout still null after Init() called.");
        }

        using var _ = PerformanceMeasurer.UseMeasurer("read-frame");

        this.ReadNextFrameFromStdout();

        if (this.stdout != null)
        {
            this.Position += 1;
            return this.lastFrame;
        }
        else
        {
            throw new MovieSharpException(MovieSharpErrorType.SubProcessFailed, "Internal ffmpeg wrapper is not started.");
        }
    }

    public ReadOnlyMemory<byte>? SeekAndRead(long frameIndex)
    {
#if DEBUG
        using var _ = PerformanceMeasurer.UseMeasurer("seek");
#endif
        var policy = "";
        var prevPosition = this.Position;

        if (this.proc is null)
        {
            policy = "re-init-proc_not_ready";
            this.log.Warn("Internal process not detected, trying to initialize...");
            this.Init(frameIndex);
            this.ReadNextFrame();
        }
        else
        {
            // Use cache.
            if (this.lastFrame is null || frameIndex < this.Position || frameIndex > this.Position + 100)
            {
                policy = "re-seek";
                this.Init(frameIndex);
                this.ReadNextFrame();
            }
            else if (this.Position == frameIndex)
            {
                policy = "use-last";
                // directly use this.lastFrame.
            }
            else
            {
                var frameCount = frameIndex - this.Position - 1;
                this.SkipFrames(frameCount);
                policy = $"fast-forward:{frameCount}";
                this.ReadNextFrame();
            }
        }

        if (policy != "fast-forward:0")
        {
            this.log.Debug($"seek to {this.Position}, from = {prevPosition}, position policy: ${policy}");
        }

        return this.LastFrame;
    }

    public unsafe void DrawFrame(SKCanvas cvs, SKPaint? paint, int frameIndex, (int x, int y) position)
    {
        this.SeekAndRead(frameIndex);

        if (this.lastFrame is null)
        {
            throw new Exception("LOGIC ERROR: lastFrame still null after Init() called.");
        }

        var lastFrame = this.lastFrame!.Value;

        using (PerformanceMeasurer.UseMeasurer("make-frame"))
        {
            try
            {
                using var handle = lastFrame.Pin();
                using var bmp = SKBitmap.FromImage(SKImage.FromPixels(this.imageInfo, (nint)handle.Pointer));
                cvs.DrawBitmap(bmp, new SKPoint(position.x, position.y), paint);
            }
            catch
            {
                this.log.Error($"Failed to draw frame from source: ${this.FileName} @ ${frameIndex}");
            }
        }

    }

    //public void MakeFrameByTime(SKBitmap frame, double t)
    //{
    //    var pos = this.GetFrameId(t);
    //    this.MakeFrameById(frame, pos);
    //}

    public string? GetErrors()
    {
        return this.stderr?.ReadToEnd();
    }

    public void Close(bool cleanup = true)
    {
        if (this.proc is not null)
        {

            if (!this.proc.HasExited)
            {
                this.proc.Kill(true);

                var errors = this.GetErrors();
                if (errors is not null)
                {
                    this.log.Warn($"ffmpeg exited, with errors:\n{errors}");
                }

                this.proc = null;
            }
        }
        if (cleanup)
        {
            this.lastFrame = null;
            GC.Collect();
        }
    }

    public void Dispose()
    {
        this.Close(true);
        GC.SuppressFinalize(this);
    }

    private string BuildScaleVF()
    {
        var ox = this.sourceResolution.X;
        var oy = this.sourceResolution.Y;
        var tx = this.targetResolution.X;
        var ty = this.targetResolution.Y;

        var or = (double)ox / oy;
        var tr = (double)tx / ty;

        if (this.FitPolicy is VideoFileSourceFitPolicy.Contain ||
            (this.FitPolicy is VideoFileSourceFitPolicy.FixedWidth && tr < or) ||
            (this.FitPolicy is VideoFileSourceFitPolicy.FixedHeight && tr > or)
           )
        {
            return $"scale={tx}:{ty}:force_original_aspect_ratio=decrease,pad={tx}:{ty}:-1:-1:color=black";
        }
        else
        {
            return $"scale={tx}:{ty}:force_original_aspect_ratio=increase,crop={tx}:{ty}";
        }
    }

    private void ReadNextFrameFromStdout()
    {
        if (this.lastFrame is null || this.stdout is null)
        {
            throw new Exception($"LOGIC ERROR: lastFrame({this.lastFrame is null}) or this.stdout({this.stdout is null}) is null while reading next frame from stdout.");
        }
        else if (this.lastFrame.Value.Length != this.bytesPerFrame)
        {
            throw new Exception($"LOGIC ERROR: Could not read frame, the length of the memory provided({this.lastFrame.Value.Length}) not as same as frameLength({this.bytesPerFrame}).");
        }

        try
        {
            this.stdout.ReadExactly(this.lastFrame.Value.Span);
        }
        catch (Exception ex)
        {
            // Use broken frame, if read failed.
            this.log.Error($"Failed to read frame @ {this.Position} / {this.FrameCount} (time: {this.Position / this.Duration: 0.00} / {this.Duration: 0.00}) for file: ${this.FileName}, use broken frame.");
            this.log.Error(ex.Message);
        }
    }

    private static Coordinate Scale(Coordinate originSize, (int?, int?)? targetResolution = null)
    {
        var targetSize = new Coordinate(originSize.X, originSize.Y);
        if (targetResolution is not null)
        {
            var (tw, th) = targetResolution.Value;
            var (w, h) = originSize;
            if (tw is null && th is not null)
            {
                var ratio = th / (double)h;

                targetSize = new Coordinate(
                    (int)(w * ratio),
                    (int)(h * ratio)
                );
            }
            else if (th is null && tw is not null)
            {
                var ratio = tw / (double)w;

                targetSize = new Coordinate(
                    (int)(w * ratio),
                    (int)(h * ratio)
                );
            }
            else if (tw is not null && th is not null)
            {
                var ratio1 = tw / (double)w;
                var ratio2 = th / (double)h;

                targetSize = new Coordinate(
                    (int)(w * ratio1),
                    (int)(h * ratio2)
                );
            }
        }

        return targetSize;
    }
}
