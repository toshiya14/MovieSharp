using MovieSharp.Objects.EncodingParameters;
using NLog;
using NLog.Fluent;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp.Targets.Videos;

internal class FFVideoFileTarget : IDisposable
{
    private readonly FFVideoParams parameters;
    private readonly string outputPath;

    public string FFMpegPath { get; }

    private readonly ILogger log = LogManager.GetCurrentClassLogger();
    private StreamWriter? stdin;
    private StreamReader? stderr;
    private Process? proc;
    private FFProgressData progress = new();

    public event EventHandler<FFProgressData>? OnProgress;

    public FFVideoFileTarget(FFVideoParams parameters, string outputPath, string ffmpegPath = "ffmpeg")
    {
        this.parameters = parameters;
        this.outputPath = outputPath;
        this.FFMpegPath = ffmpegPath;
    }

    public void Init()
    {
        var arglist = new List<string>() { 
            // below: input parameters.
            "-y",
            "-progress", "pipe:1",
            "-loglevel", "error",
            "-f", "rawvideo",
            "-vcodec", "rawvideo",
            "-s", $"{parameters.Size!.X}x{parameters.Size!.Y}",
            "-pix_fmt", parameters.SourcePixfmt.ToFFPixfmt(),
            "-r", parameters.FrameRate!.Value.ToString("0.000"),
            "-an",
            "-i",
            "-",
        };

        if (parameters.WithCopyAudio is not null)
        {
            arglist.AddRange(new[] {
                "-i", $"\"{parameters.WithCopyAudio}\"",
                "-acodec", "copy"
            });
        }

        // below: encoding parameters
        arglist.AddRange(new[] {
            "-vcodec", parameters.Codec,
            "-preset", parameters.Preset
        });

        if (parameters.Bitrate is not null)
        {
            arglist.AddRange(new[] {
                "-b", parameters.Bitrate
            });
        }
        else if (parameters.CRF is not null)
        {
            arglist.AddRange(new[] {
                "-crf", parameters.CRF!.Value.ToString()
            });
        }

        arglist.AddRange(new[] {
            "-pix_fmt", parameters.TargetPixfmt
        });

        // below: filename
        var fi = new FileInfo(outputPath);
        if (fi.Directory?.Exists == false)
        {
            fi.Directory!.Create();
        }
        arglist.Add($"\"{fi.FullName}\"");

        // create thread
        var args = string.Join(' ', arglist);
        log.Info("Generated args: " + args);

        this.proc = new Process();
        proc.StartInfo.FileName = FFMpegPath;
        proc.StartInfo.Arguments = args;
        proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        proc.StartInfo.RedirectStandardError = true;
        proc.StartInfo.RedirectStandardInput = true;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.OutputDataReceived += StdoutReceived;
        proc.StartInfo.UseShellExecute = false;
        proc.Start();

        this.stdin = proc.StandardInput;
        this.stderr = proc.StandardError;

        this.progress = new FFProgressData();
        proc.BeginOutputReadLine();
    }

    private void StdoutReceived(object sender, DataReceivedEventArgs e)
    {
        if (this.OnProgress is null)
        {
            return;
        }
        if (e.Data is null)
        {
            return;
        }

        var line = e.Data.Trim();
        var parts = line.Split("=", 2);
        if (parts.Length != 2)
        {
            return;
        }
        var key = parts[0];
        var value = parts[1];

        switch (key)
        {
            case "progress":
                if (value == "continue")
                {
                    this.progress.Progress = FFProgressState.Continue;
                }
                else if (value == "end")
                {
                    this.progress.Progress = FFProgressState.End;
                }

                this.OnProgress.Invoke(this, this.progress!);
                break;

            case "frame":
                this.progress.Frame = Convert.ToInt64(value);
                break;

            case "fps":
                this.progress.Fps = Convert.ToSingle(value);
                break;

            case "bitrate":
                this.progress.Bitrate = value;
                break;

            case "total_size":
                this.progress.TotalSize = Convert.ToInt64(value);
                break;

            case "speed":
                if (value == "N/A") {
                    break;
                }
                this.progress.Speed = Convert.ToSingle(value[..^1]);
                break;
        }
    }

    public void WriteFrame(Memory<byte> frame)
    {
        if (this.proc is null || this.stdin is null)
        {
            throw new NotSupportedException("The subprocess has not been initialized.");
        }
        if (this.proc.HasExited)
        {
            throw new OperationCanceledException(this.GetErrors());
        }
        this.stdin.BaseStream.Write(frame.Span);
    }

    public void Close()
    {
        try
        {
            this.stdin?.Flush();
            this.stdin?.Close();
        }
        catch
        {
            // ignore.
        }
    }

    public string? GetErrors()
    {
        this.Close();
        return this.stderr?.ReadToEnd();
    }

    public void Dispose()
    {
        this.Close();
        this.proc?.WaitForExit();
        this.proc?.Kill(true);
    }
}
