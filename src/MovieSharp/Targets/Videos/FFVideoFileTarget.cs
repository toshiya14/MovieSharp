using System.Diagnostics;
using System.Text.RegularExpressions;
using MovieSharp.Objects.EncodingParameters;
using NLog;

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

    private Regex reProgress = new Regex(@"frame=\s*([0-9]+)\s+fps=\s*([0-9\.]+)\s+q=\s*([0-9\.]+)\s+L?size=\s*(.*?)\s+time=\s*(.*?)\s+bitrate=\s*(.*?)\s+speed=\s*([0-9\.x]+)");

    public event EventHandler<FFProgressData>? OnProgress;

    public FFVideoFileTarget(FFVideoParams parameters, string outputPath, string ffmpegPath = "ffmpeg")
    {
        this.parameters = parameters;
        this.outputPath = outputPath;
        this.FFMpegPath = ffmpegPath;
    }

    public void Init()
    {
        var arglist = new List<string>(32) {
            // below: input parameters.
            "-y",
            "-progress", "pipe:1",
            "-threads", $"{this.parameters.Threads}",
            "-loglevel", "error",
            "-f", "rawvideo",
            "-vcodec", "rawvideo",
            "-s", $"{this.parameters.Size!.X}x{this.parameters.Size!.Y}",
            "-pix_fmt", "rgba",
            "-r", this.parameters.FrameRate!.Value.ToString("0.000"),
            "-an",
            "-i",
            "-",
        };

        if (this.parameters.WithCopyAudio is not null)
        {
            arglist.AddRange([
                "-i", $"\"{this.parameters.WithCopyAudio}\"",
                "-acodec", "copy"
            ]);
        }

        // below: encoding parameters
        arglist.AddRange([
            "-vcodec", this.parameters.Codec,
            "-preset", this.parameters.Preset,
            "-bf", $"{this.parameters.BF}",
            "-color_range:v", "tv",
            "-colorspace", "bt709",
            "-color_primaries:v", "bt709",
            "-profile:v", "main",
            "-map_metadata", "0",
            "-movflags", "use_metadata_tags"
        ]);

        if (this.parameters.Bitrate is not null)
        {
            // CBR / VBR
            arglist.AddRange([
                "-b:v", this.parameters.Bitrate
            ]);

            if (this.parameters.Maxrate is not null)
            {
                // VBR
                arglist.AddRange(["-maxrate", this.parameters.Maxrate]);
                arglist.AddRange(["-maxrate", this.parameters.Maxrate]);
            }
        }
        else if (this.parameters.CRF is not null)
        {
            // CRF
            arglist.AddRange(["-crf", this.parameters.CRF!.Value.ToString()]);
        }

        arglist.AddRange(["-pix_fmt", this.parameters.TargetPixfmt]);

        // below: metadata
        foreach (var (key, value) in this.parameters.Metadata) {
            arglist.AddRange(["-metadata", $"{key}='{value}'"]);
        }

        // below: filename
        var fi = new FileInfo(this.outputPath);
        if (fi.Directory?.Exists == false)
        {
            fi.Directory!.Create();
        }
        arglist.AddRange(["-f", "mp4"]);
        arglist.Add($"\"{fi.FullName}.tmp\"");

        // create thread
        var args = string.Join(' ', arglist);
        this.log.Info("Generated args: " + args);

        this.proc = new Process();
        this.proc.StartInfo.FileName = this.FFMpegPath;
        this.proc.StartInfo.Arguments = args;
        this.proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        this.proc.StartInfo.RedirectStandardError = true;
        this.proc.StartInfo.RedirectStandardInput = true;
        this.proc.StartInfo.RedirectStandardOutput = true;
        this.proc.OutputDataReceived += this.StdoutReceived;
        this.proc.ErrorDataReceived += this.StderrReceived; ;
        this.proc.StartInfo.UseShellExecute = false;
        this.proc.StartInfo.CreateNoWindow = true;
        this.proc.Start();

        this.stdin = this.proc.StandardInput;
        this.stderr = this.proc.StandardError;

        this.progress = new FFProgressData();
        this.proc.BeginOutputReadLine();
    }

    private void StderrReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null)
        {
            return;
        }

        var progMatches = this.reProgress.Match(e.Data);
        if (!progMatches.Success)
        {
            this.log.Info(@$"[ffmpeg.exe] {e.Data}");
        }
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
                if (value == "N/A")
                {
                    break;
                }
                this.progress.Speed = Convert.ToSingle(value[..^1]);
                break;
        }
    }

    public void WriteFrame(ReadOnlySpan<byte> frame)
    {
        if (this.proc is null || this.stdin is null)
        {
            throw new NotSupportedException("The subprocess has not been initialized.");
        }
        if (this.proc.HasExited)
        {
            throw new OperationCanceledException(this.GetErrors());
        }
        this.stdin.BaseStream.Write(frame);
    }

    public void Finish()
    {
        this.proc?.WaitForExit();
        this.proc?.Kill(true);
        this.proc = null;

        var fi = new FileInfo(this.outputPath + ".tmp");
        if (fi.Exists)
        {
            fi.MoveTo(this.outputPath);
        }
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

        // clean up
        var fi = new FileInfo(this.outputPath + ".tmp");
        if (fi.Exists)
        {
            fi.Delete();
        }
    }
}
