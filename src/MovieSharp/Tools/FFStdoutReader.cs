using NLog;

namespace MovieSharp.Tools;

internal class FFStdoutReader
{
    private readonly Stream stdout;
    private readonly ILogger log = LogManager.GetCurrentClassLogger();
    private readonly int frameLength;

    public FFStdoutReader(Stream stdout, int frameLength)
    {
        this.stdout = stdout;
        this.frameLength = frameLength;
    }

    public (Memory<byte>? buffer, int readedCount) ReadNextFrame()
    {
        var offset = 0;
        var reader = new StreamReader(this.stdout);
        //var sw = Stopwatch.StartNew();
        var buffer = new byte[this.frameLength];

        while (offset < this.frameLength)
        {
            var r = this.stdout.Read(buffer, offset, this.frameLength - offset);
            if (r <= 0)
            {
                if (reader.EndOfStream)
                {
                    break;
                }
            }
            offset += r;
        }

        if (offset != this.frameLength)
        {
            return (null, offset);
        }

        //sw.Stop();
        //log.Debug($"ReadNextFrame Finished: {sw.Elapsed}");

        return (buffer.AsMemory(), offset);
    }
}
