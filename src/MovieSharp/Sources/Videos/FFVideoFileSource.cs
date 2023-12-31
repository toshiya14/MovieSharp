﻿using System.Diagnostics;
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

internal class FFVideoFileSource : IVideoSource
{
    private Process? proc;
    private ILogger log = LogManager.GetCurrentClassLogger();
    private Stream? stdout;
    private StreamReader? stderr;
    private FFStdoutReader? stdoutReader;
    private (int?, int?)? targetResolution = null;
    private SKImageInfo imageInfo;
    private int bytesPerFrame;

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

        // TODO: temporary not support rotate.
    }

    /// <summary>
    /// Opens the file, creates the pipe.
    /// Sets `Position` to the appropriate value (1 if startTime == 0 because
    /// it pre-reads the first frame).
    /// </summary>
    /// <param name="startTime"></param>
    public void Init(int startIndex = 0)
    {
        using var _ = PerformanceMeasurer.UseMeasurer("init");

        this.Infos = FFProbe.Analyse(this.FileName, new FFOptions { BinaryFolder = this.FFMpegBinFolder });

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

        if (this.Duration == 0 && this.Infos.VideoStreams.Count > 0)
        {
            this.Duration = this.Infos.Duration.TotalSeconds;
        }

        if (this.Duration == 0)
        {
            throw new MovieSharpException(MovieSharpErrorType.ResourceLoadingFailed, "Could not determine the duration of the media, maybe it do not contains video stream.");
        }

        this.FrameCount = (int)(vidstream.FrameRate * this.Duration);
        this.imageInfo = new SKImageInfo(this.Size.X, this.Size.Y, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        this.bytesPerFrame = this.Size.X * this.Size.Y * this.PixelChannels;

        // If there is an running or suspending task, terminate it.
        this.Close(false);
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
            "-vf", $"scale={this.Size.X}:{this.Size.Y}",
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
    }
    public void SkipFrames(int n = 1)
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
    public int GetFrameId(double time) => (int)(this.FrameRate * time + 0.000001);
    public Memory<byte>? ReadNextFrame()
    {
        using var _ = PerformanceMeasurer.UseMeasurer("read-frame");

        if (this.stdout != null)
        {
            var readed = this.stdoutReader?.ReadNextFrame();
            if (readed?.buffer is null)
            {
                this.log.Error($"In file {this.FileName}, {this.bytesPerFrame} bytes wanted but read {readed?.readedCount} bytes at frame index: {this.Position} (out of a total {this.FrameCount} frames), at time {this.Position / this.FrameRate:0.00}/{this.Duration:0.00}");
                if (this.LastFrame is null)
                {
                    throw new MovieSharpException(MovieSharpErrorType.SubProcessFailed, $"failed to read the first frame of video file {this.FileName}. That might mean that the file is corrupted. That may also mean that you are using a deprecated version of FFMPEG. On Ubuntu/Debian for instance the version in the repos is deprecated. Please update to a recent version from the website.");
                }
                this.Position += 1;
                return readed?.buffer;
            }
            else
            {
                // install pixels
                if (readed?.buffer.Value.Length != this.bytesPerFrame)
                {
                    throw new MovieSharpException(MovieSharpErrorType.SubProcessFailed, $"MakeFrameByTime returns {readed?.buffer.Value.Length} bytes but {this.bytesPerFrame} wanted. Maybe the pixel format is not correct.");
                }

                this.Position += 1;
                return readed?.buffer;
            }
        }
        else
        {
            throw new MovieSharpException(MovieSharpErrorType.SubProcessFailed, "Internal ffmpeg wrapper is not started.");
        }
    }

    public SKBitmap? MakeFrameById(int frameIndex)
    {
#if DEBUG
        using var _ = PerformanceMeasurer.UseMeasurer("make-frame");
#endif
        //this.log.Trace($"MakeFrame for {this.FileName}: {frameIndex} / {this.FrameCount}");
        // Initialize proc if it is not open
        if (this.proc is null)
        {
            this.log.Warn("Internal process not detected, trying to initialize...");
            this.Init();
            this.LastFrame = this.ReadNextFrame();
        }
        else
        {
            // Use cache.
            if (this.Position == frameIndex && this.LastFrame != null)
            {
                // directly use this.LastFrame.
            }
            else if (frameIndex < this.Position || frameIndex > this.Position + 100)
            {
                // seek to specified frame would takes too long.
                this.Init(frameIndex);
                this.LastFrame = this.ReadNextFrame();
            }
            else
            {
                this.SkipFrames(frameIndex - this.Position - 1);
                this.LastFrame = this.ReadNextFrame();
            }
        }

        if (this.LastFrame != null)
        {
            using (PerformanceMeasurer.UseMeasurer("make-frame-extract"))
            {
                var bitmap = new SKBitmap(this.imageInfo);
                using var img = SKImage.FromPixelCopy(this.imageInfo, this.LastFrame.Value.Span);
                return SKBitmap.FromImage(img);
            }
        }
        else
        {
            return null;
        }
    }

    public SKBitmap? MakeFrameByTime(double t)
    {
        var pos = this.GetFrameId(t);
        var frame = this.MakeFrameById(pos);
        return frame;
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
            this.LastFrame = null;
            GC.Collect();
        }
    }

    public void Dispose()
    {
        this.Close(true);
        GC.SuppressFinalize(this);
    }
}
