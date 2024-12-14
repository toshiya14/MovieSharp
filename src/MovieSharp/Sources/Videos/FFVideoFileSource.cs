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

internal class FFVideoFileSource : IVideoSource
{
    private Process? proc;
    private ILogger log = LogManager.GetCurrentClassLogger();
    private Stream? stdout;
    private StreamReader? stderr;
    private FFStdoutReader? stdoutReader;
    private Coordinate targetResolution;
    private SKImageInfo imageInfo;
    private int bytesPerFrame;

    public VideoFileSourceFitPolicy FitPolicy { get; }
    public string FileName { get; }
    public double FrameRate { get; private set; }
    public double Duration { get; private set; }
    public Coordinate Size { get; private set; }
    public int FrameCount { get; private set; }
    public int Position { get; private set; }
    public Memory<byte>? LastFrame { get; private set; }
    public int PixelChannels { get; private set; } = 4;
    public PixelFormat PixelFormat { get; } = PixelFormat.RGBA32;
    public string ResizeAlgo { get; set; } = "bicubic";
    public IMediaAnalysis? Infos { get; private set; }
    public string FFMpegPath { get; set; } = "ffmpeg";

    /// <summary>
    /// Use PATH if set to empty.
    /// </summary>
    public string FFMpegBinFolder { get; set; } = string.Empty;

    public FFVideoFileSource(string filename, VideoFileSourceFitPolicy fitPolicy, (int?, int?)? resolution = null)
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
        this.Infos = FFProbe.Analyse(this.FileName, new FFOptions { BinaryFolder = this.FFMpegBinFolder });

        if (this.Infos.VideoStreams.Count < 1)
        {
            throw new Exception($"{this.FileName} does contain any video stream.");
        }
        var vidstream = this.Infos.VideoStreams[0];

        this.FrameRate = vidstream.FrameRate;
        this.Size = new Coordinate(vidstream.Width, vidstream.Height);

        // re-calculate target resolution.
        this.targetResolution = Scale(this.Size, resolution);
        this.bytesPerFrame = this.targetResolution.X * this.targetResolution.Y * this.PixelChannels;

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
    /// Sets `Position` to the appropriate value (1 if startTime == 0 because
    /// it pre-reads the first frame).
    /// </summary>
    /// <param name="startTime"></param>
    private void Init(int startIndex = 0)
    {
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

        arglist.AddRange(new string[] {
            "-loglevel", "error",
            "-f", "image2pipe",
            "-vf", this.BuildVF(),
            "-sws_flags", this.ResizeAlgo,
            "-pix_fmt", "rgba",
            "-vcodec", "rawvideo",
            "-"
        });

        var args = string.Join(' ', arglist);
        this.log.Info("Generated args: " + args);

        this.proc = new Process();
        this.proc.StartInfo.FileName = this.FFMpegPath;
        this.proc.StartInfo.Arguments = args;
        this.proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        this.proc.StartInfo.RedirectStandardOutput = true;
        this.proc.StartInfo.RedirectStandardError = true;
        this.proc.StartInfo.UseShellExecute = false;
        this.proc.StartInfo.CreateNoWindow = true;
        this.proc.Start();

        this.stdout = this.proc.StandardOutput.BaseStream;
        this.stderr = this.proc.StandardError;
        this.stdoutReader = new FFStdoutReader(this.stdout, this.bytesPerFrame);

        this.Position = startIndex;
        this.LastFrame = (new byte[this.bytesPerFrame]).AsMemory();
    }

    public void SkipFrames(int n = 1)
    {
        if (this.LastFrame is null)
        {
            // not alloc memory
            this.Init(0);
        }

        if (this.LastFrame is null)
        {
            throw new Exception("LOGIC ERROR: LastFrame still null after Init() called.");
        }

        var frame = this.LastFrame!.Value;

        if (this.stdoutReader != null)
        {
            for (long i = 0; i < n; i++)
            {
                this.stdoutReader.ReadNextFrame(frame);
            }
            this.Position += n;
        }
    }

    public int GetFrameId(double time) => (int)(this.FrameRate * time + 0.000001);

    public Memory<byte>? ReadNextFrame()
    {
        if (this.LastFrame is null)
        {
            // not alloc memory
            this.Init(0);
        }

        if (this.LastFrame is null)
        {
            throw new Exception("LOGIC ERROR: LastFrame still null after Init() called.");
        }

        var frame = this.LastFrame!.Value;

        using var _ = PerformanceMeasurer.UseMeasurer("read-frame");

        if (this.stdout != null)
        {
            var readed = this.stdoutReader?.ReadNextFrame(frame);
            if (readed == 0)
            {
                this.log.Error($"In file {this.FileName}, {this.bytesPerFrame} bytes wanted but read 0 bytes at frame index: {this.Position} (out of a total {this.FrameCount} frames), at time {this.Position / this.FrameRate:0.00}/{this.Duration:0.00}");
            }

            this.Position += 1;
            return this.LastFrame;
        }
        else
        {
            throw new MovieSharpException(MovieSharpErrorType.SubProcessFailed, "Internal ffmpeg wrapper is not started.");
        }
    }

    public unsafe void DrawFrame(SKCanvas cvs, int frameIndex, (int x, int y) position)
    {
#if DEBUG
        using var _ = PerformanceMeasurer.UseMeasurer("make-frame");
#endif
        var policy = "";


        if (this.proc is null)
        {
            policy = "re-init-proc_not_ready";
            this.log.Warn("Internal process not detected, trying to initialize...");
            this.Init();
            this.ReadNextFrame();
        }
        else
        {
            // Use cache.
            if (this.LastFrame is null || frameIndex < this.Position || frameIndex > this.Position + 100)
            {
                policy = "re-seek";
                this.Init(frameIndex);
                this.ReadNextFrame();
            }
            else if (this.Position == frameIndex)
            {
                policy = "use-last";
                // directly use this.LastFrame.
            }
            else
            {
                policy = "fast-forward";
                this.SkipFrames(frameIndex - this.Position - 1);
                this.ReadNextFrame();
            }
        }

        if (this.LastFrame is null)
        {
            throw new Exception("LOGIC ERROR: LastFrame still null after Init() called.");
        }

        var lastFrame = this.LastFrame!.Value;

        using (PerformanceMeasurer.UseMeasurer("make-frame-extract"))
        {
            try
            {
                using var handle = lastFrame.Pin();
                using var bmp = SKBitmap.FromImage(SKImage.FromPixels(this.imageInfo, (nint)handle.Pointer));
                cvs.DrawBitmap(bmp, new SKPoint(position.x, position.y));
            }
            catch
            {
                this.log.Error($"Failed to draw frame from source: ${this.FileName} @ ${frameIndex}, position policy: ${policy}");
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
                this.proc = null;
            }
        }
        if (cleanup)
        {
            this.LastFrame = null;
            GC.Collect();
        }
    }

    public void Dispose()
    {
        this.Close(true);
        GC.SuppressFinalize(this);
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

    private string BuildVF()
    {
        var ox = this.Size.X;
        var oy = this.Size.Y;
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
}
