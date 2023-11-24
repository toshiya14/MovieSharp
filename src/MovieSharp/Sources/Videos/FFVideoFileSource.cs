using FFMpegCore;
using MovieSharp.Objects;
using MovieSharp.Tools;
using NLog;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        // initialize members;
        FileName = filename;
        proc = null;
        targetResolution = resolution;
        Size = new Coordinate(0, 0);
        PixelFormat = PixelFormat.RGBA32;

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
        Infos = FFProbe.Analyse(FileName);

        if (Infos.VideoStreams.Count < 1)
        {
            throw new Exception($"{FileName} does contain any video stream.");
        }
        var vidstream = Infos.VideoStreams[0];

        FrameRate = vidstream.FrameRate;
        Size = new Coordinate(vidstream.Width, vidstream.Height);

        if (targetResolution != null)
        {
            var (tw, th) = targetResolution.Value;
            var (w, h) = Size;
            if (tw is null && th is not null)
            {
                var ratio = th / (double)h;

                Size = new Coordinate(
                    (int)(w * ratio),
                    (int)(h * ratio)
                );
            }
            else if (th is null && tw is not null)
            {
                var ratio = tw / (double)w;

                Size = new Coordinate(
                    (int)(w * ratio),
                    (int)(h * ratio)
                );
            }
            else if (tw is not null && th is not null)
            {
                var ratio1 = tw / (double)w;
                var ratio2 = th / (double)h;

                Size = new Coordinate(
                    (int)(w * ratio1),
                    (int)(h * ratio2)
                );
            }
        }

        Duration = vidstream.Duration.TotalSeconds;
        FrameCount = (long)(vidstream.FrameRate * vidstream.Duration.TotalSeconds);
        PixelChannels = PixelFormat == PixelFormat.RGB24 || PixelFormat == PixelFormat.BGR24 ? 3 : 4;

        // If there is an running or suspending task, terminate it.
        Close(false);
        var arglist = new List<string>();
        var startTime = FrameRate * startIndex;

        double offset;
        if (startTime != 0)
        {
            offset = Math.Min(1, startTime);
            arglist.AddRange(new string[] {
                "-ss",
                (startTime - offset).ToString("f6"),
                "-i",
                $"\"{FileName}\"",
                "-ss",
                offset.ToString("f6"),
            });
        }
        else
        {
            arglist.AddRange(new string[] {
                "-i",
                $"\"{FileName}\"",
            });
        }

        arglist.AddRange(new string[] {
            "-loglevel",
            "error",
            "-f",
            "image2pipe",
            "-vf",
            $"scale={Size.X}:{Size.Y}",
            "-sws_flags",
            ResizeAlgo,
            "-pix_fmt",
            PixfmtToString(PixelFormat),
            "-vcodec",
            "rawvideo",
            "-"
        });

        var args = string.Join(' ', arglist);
        log.Info("Generated args: " + args);

        proc = new Process();
        proc.StartInfo.FileName = FFMpegPath;
        proc.StartInfo.Arguments = args;
        proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.RedirectStandardError = true;
        proc.StartInfo.UseShellExecute = false;
        proc.Start();

        var (width, height) = Size;
        var bytesToRead = PixelChannels * width * height;
        this.stdout = proc.StandardOutput.BaseStream;
        this.stderr = proc.StandardError;
        stdoutReader = new FFStdoutReader(this.stdout, bytesToRead);

        Position = startIndex;
        LastFrameRaw = ReadNextFrame();
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
        if (stdoutReader != null)
        {
            for (long i = 0; i < n; i++)
            {
                stdoutReader.ReadNextFrame();
            }
            Position += n;
        }
    }

    public long GetFrameNumber(double time)
    {
        return (long)(FrameRate * time + 0.00001);
    }

    public Memory<byte> ReadNextFrame()
    {
        var (width, height) = Size;
        var bytesToRead = PixelChannels * width * height;

        var stdout = proc?.StandardOutput.BaseStream;

        Memory<byte> result;
        if (stdout != null)
        {
            var reader = new FFStdoutReader(stdout, bytesToRead);

            var buffer = reader.ReadNextFrame();
            if (buffer is null)
            {
                Trace.TraceWarning($"In file {FileName}, {bytesToRead} bytes wanted but not enough bytes read at frame index: {Position} (out of a total {FrameCount} frames), at time {Position / FrameRate:0.00}/{Duration:0.00}");
                if (LastFrameRaw is null)
                {
                    throw new IOException($"failed to read the first frame of video file {FileName}. That might mean that the file is corrupted. That may also mean that you are using a deprecated version of FFMPEG. On Ubuntu/Debian for instance the version in the repos is deprecated. Please update to a recent version from the website.");
                }
                result = LastFrameRaw.Value;
            }
            else
            {
                result = buffer.Value;
                LastFrameRaw = buffer;
            }
        }
        else
        {
            throw new NotSupportedException("Internal ffmpeg wrapper is not started.");
        }

        Position += 1;
        return result;
    }

    public Memory<byte>? MakeFrame(long frameIndex)
    {
        // Initialize proc if it is not open
        if (proc is null)
        {
            Trace.TraceWarning("Internal process not detected, trying to initialize...");
            Init();
            return LastFrameRaw!.Value;
        }

        // Use cache.
        if (Position == frameIndex && LastFrameRaw != null)
        {
            return LastFrameRaw!.Value;
        }
        else if (frameIndex < Position || frameIndex > Position + 100)
        {
            // seek to specified frame would takes too long.
            Init(frameIndex);
            return LastFrameRaw!.Value;
        }
        else
        {
            SkipFrames(frameIndex - Position - 1);
            return ReadNextFrame();
        }
    }

    public Memory<byte>? MakeFrameByTime(double t)
    {
        // + 1 so that it represents the frame position that it will be
        // after the frame is read. This makes the later comparisons easier.
        var pos = GetFrameNumber(t) + 1;
        return MakeFrame(pos);
    }

    public string? GetErrors()
    {
        return this.stderr?.ReadToEnd();
    }

    public void Close(bool cleanup = true)
    {
        if (proc is not null)
        {
            if (!proc.HasExited)
            {
                proc.Kill(true);
                proc = null;
            }
        }
        if (cleanup)
        {
            LastFrameRaw = null;
            GC.Collect();
        }
    }

    public void Dispose()
    {
        Close(true);
        GC.SuppressFinalize(this);
    }
}
