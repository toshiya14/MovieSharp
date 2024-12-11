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

    public int ReadNextFrame(Memory<byte> frame)
    {
        var offset = 0;
        var buffer = new byte[1024];

        while (offset < frameLength)
        {
            var rest = frameLength - offset;
            if (rest > buffer.Length)
            {
                rest = buffer.Length;
            }
            else
            {
                buffer = new byte[rest];
            }
            var r = this.stdout.Read(buffer);

            if (r <= 0)
            {
                if (offset == 0) return 0;
                else break;
            }

            var span = frame.Slice(offset, r);
            buffer.CopyTo(span);

            offset += r;
        }

        return offset;
    }
}
