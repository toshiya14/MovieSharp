using System.Drawing;
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
        var buf = new byte[frameLength];

        while (offset < frameLength)
        {
            var r = this.stdout.Read(buf, offset, frameLength - offset);
            if (r <= 0)
            {
                if (offset == 0) return (null, 0);
                else break;
            }

            offset += r;
        }

        // Adjust RawData length when changed
        if (buf.Length != offset)
        {
            return (buf.AsMemory()[..offset], offset);
        }
        else
        {
            return (buf.AsMemory(), buf.Length);
        }
    }
}
