using System.Diagnostics;
using FFMpegCore;
using MovieSharp.Exceptions;
using MovieSharp.Objects;
using MovieSharp.Tools;
using NLog;

// This is a port from:
// https://github.com/Zulko/moviepy/blob/9ebabda20a27780101f5c6c832ed398d28377726/moviepy/video/io/ffmpeg_reader.py

namespace MovieSharp.Sources.Videos;

internal class FFVideoFileSource : IVideoSource
{
    private Process? proc;
    private ILogger log = LogManager.GetCurrentClassLogger();
    private Stream? stdout;
    private StreamReader? stderr;
    private FFStdoutReader? stdoutReader;

    public string FileName { get; }
    public double FrameRate { get; private set; }
    public double Duration { get; private set; }
    public Coordinate Size { get; private set; }
    public long FrameCount { get; private set; }
    public long Position { get; private set; }
    public Memory<byte>? LastFrameRaw { get; private set; }
    public int PixelChannels { get; private set; }
    public PixelFormat PixelFormat { get; set; }
    public string ResizeAlgo { get; set; } = "bicubic";
    public IMediaAnalysis? Infos { get; private set; }
    public string FFMpegPath { get; set; } = "ffmpeg";
    private (int?, int?)? targetResolution = null;

    public FFVideoFileSource(string filename, (int?, int?)? resolution = null)
    {
        var fi = new FileInfo(filename);
        if (!fi.Exists)
        {
            throw new MovieSharpException(MovieSharpErrorType.ResourceNotFound, $"Not found: {fi.FullName}");
        }

        // initialize members;
        this.FileName = filename;
        this.proc = null;
        this.targetResolution = resolution;
        this.Size = new Coordinate(0, 0);
        this.PixelFormat = PixelFormat.RGBA32;

        // TODO: temporary not support rotate.
    }

    /// <summary>
    /// Opens the file, creates the pipe.
    /// Sets `Position` to the appropriate value (1 if startTime == 0 because
    /// it pre-reads the first frame).
    /// </summary>
    /// <param name="startTime"></param>
    public void Init(long startIndex = 0)
    {
        this.Infos = FFProbe.Analyse(this.FileName);

        if (this.Infos.VideoStreams.Count < 1)
        {
            throw new Exception($"{this.FileName} does contain any video stream.");
        }
        var vidstream = this.Infos.VideoStreams[0];

        this.FrameRate = vidstream.FrameRate;
        this.Size = new Coordinate(vidstream.Width, vidstream.Height);

        if (this.targetResolution != null)
        {
            var (tw, th) = this.targetResolution.Value;
            var (w, h) = this.Size;
            if (tw is null && th is not null)
            {
                var ratio = th / (double)h;

                this.Size = new Coordinate(
                    (int)(w * ratio),
                    (int)(h * ratio)
                );
            }
            else if (th is null && tw is not null)
            {
                var ratio = tw / (double)w;

                this.Size = new Coordinate(
                    (int)(w * ratio),
                    (int)(h * ratio)
                );
            }
            else if (tw is not null && th is not null)
            {
                var ratio1 = tw / (double)w;
                var ratio2 = th / (double)h;

                this.Size = new Coordinate(
                    (int)(w * ratio1),
                    (int)(h * ratio2)
                );
            }
        }

        this.Duration = vidstream.Duration.TotalSeconds;
        this.FrameCount = (long)(vidstream.FrameRate * vidstream.Duration.TotalSeconds);
        this.PixelChannels = this.PixelFormat == PixelFormat.RGB24 || this.PixelFormat == PixelFormat.BGR24 ? 3 : 4;

        // If there is an running or suspending task, terminate it.
        this.Close(false);
        var arglist = new List<string>();
        var startTime = startIndex / this.FrameRate;

        double offset;
        if (startTime != 0)
        {
            offset = Math.Min(1, startTime);
            arglist.AddRange(new string[] {
                "-ss",
                (startTime - offset).ToString("f6"),
                "-i",
                $"\"{this.FileName}\"",
                "-ss",
                offset.ToString("f6"),
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
            "-loglevel",
            "error",
            "-f",
            "image2pipe",
            "-vf",
            $"scale={this.Size.X}:{this.Size.Y}",
            "-sws_flags",
            this.ResizeAlgo,
            "-pix_fmt",
            PixfmtToString(this.PixelFormat),
            "-vcodec",
            "rawvideo",
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

        var (width, height) = this.Size;
        var bytesToRead = this.PixelChannels * width * height;
        this.stdout = this.proc.StandardOutput.BaseStream;
        this.stderr = this.proc.StandardError;
        this.stdoutReader = new FFStdoutReader(this.stdout, bytesToRead);

        this.Position = startIndex;
        this.LastFrameRaw = this.ReadNextFrame();
    }

    public static string PixfmtToString(PixelFormat pixfmt)
    {
        if (pixfmt == PixelFormat.ARGB32)
        {
            return "argb";
        }
        else if (pixfmt == PixelFormat.RGBA32)
        {
            return "rgba";
        }
        else if (pixfmt == PixelFormat.BGRA32)
        {
            return "bgra";
        }
        else if (pixfmt == PixelFormat.RGB24)
        {
            return "rgb24";
        }
        else if (pixfmt == PixelFormat.BGR24)
        {
            return "bgr24";
        }
        else
        {
            throw new NotSupportedException($"Unsupported pixfmt: {pixfmt}");
        }
    }

    public void SkipFrames(long n = 1)
    {
        if (this.stdoutReader != null)
        {
            for (long i = 0; i < n; i++)
            {
                this.stdoutReader.ReadNextFrame();
            }
            this.Position += n;
        }
    }

    public long GetFrameNumber(double time)
    {
        return (long)(this.FrameRate * time + 0.00001);
    }

    public Memory<byte> ReadNextFrame()
    {
        var (width, height) = this.Size;
        var bytesToRead = this.PixelChannels * width * height;

        var stdout = this.proc?.StandardOutput.BaseStream;

        Memory<byte> result;
        if (stdout != null)
        {
            var reader = new FFStdoutReader(stdout, bytesToRead);

            var buffer = reader.ReadNextFrame();
            if (buffer is null)
            {
                Trace.TraceWarning($"In file {this.FileName}, {bytesToRead} bytes wanted but not enough bytes read at frame index: {this.Position} (out of a total {this.FrameCount} frames), at time {this.Position / this.FrameRate:0.00}/{this.Duration:0.00}");
                if (this.LastFrameRaw is null)
                {
                    throw new IOException($"failed to read the first frame of video file {this.FileName}. That might mean that the file is corrupted. That may also mean that you are using a deprecated version of FFMPEG. On Ubuntu/Debian for instance the version in the repos is deprecated. Please update to a recent version from the website.");
                }
                result = this.LastFrameRaw.Value;
            }
            else
            {
                result = buffer.Value;
                this.LastFrameRaw = buffer;
            }
        }
        else
        {
            throw new NotSupportedException("Internal ffmpeg wrapper is not started.");
        }

        this.Position += 1;
        return result;
    }

    public Memory<byte>? MakeFrame(long frameIndex)
    {
        // Initialize proc if it is not open
        if (this.proc is null)
        {
            Trace.TraceWarning("Internal process not detected, trying to initialize...");
            this.Init();
            return this.LastFrameRaw!.Value;
        }

        // Use cache.
        if (this.Position == frameIndex && this.LastFrameRaw != null)
        {
            return this.LastFrameRaw!.Value;
        }
        else if (frameIndex < this.Position || frameIndex > this.Position + 100)
        {
            // seek to specified frame would takes too long.
            this.Init(frameIndex);
            return this.LastFrameRaw!.Value;
        }
        else
        {
            this.SkipFrames(frameIndex - this.Position - 1);
            return this.ReadNextFrame();
        }
    }

    public Memory<byte>? MakeFrameByTime(double t)
    {
        // + 1 so that it represents the frame position that it will be
        // after the frame is read. This makes the later comparisons easier.
        var pos = this.GetFrameNumber(t) + 1;
        return this.MakeFrame(pos);
    }

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
            this.LastFrameRaw = null;
            GC.Collect();
        }
    }

    public void Dispose()
    {
        this.Close(true);
        GC.SuppressFinalize(this);
    }
}
