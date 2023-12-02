using NLog;

namespace MovieSharp.Tools;

internal class FFStdoutReader
{
    private readonly Stream stdout;
    //private readonly ILogger log = LogManager.GetCurrentClassLogger();
    private readonly int frameLength;

    public FFStdoutReader(Stream stdout, int frameLength)
    {
        this.stdout = stdout;
        this.frameLength = frameLength;
    }

    public async Task<Memory<byte>?> ReadNextFrame()
    {
        var offset = 0;
        //var sw = Stopwatch.StartNew();
        //var buffer = new byte[this.frameLength];
        var buffer = new Memory<byte>(new byte[this.frameLength]);

        while (offset < this.frameLength)
        {
            var r = await this.stdout.ReadAsync(buffer);
            if (r <= 0)
            {
                if (offset == 0)
                {
                    return null;
                }
                else
                {
                    break;
                }
            }
            offset += r;
        }

        if (offset != this.frameLength)
        {
            return null;
        }

        //sw.Stop();
        //log.Debug($"ReadNextFrame Finished: {sw.Elapsed}");

        return buffer;
    }
}
