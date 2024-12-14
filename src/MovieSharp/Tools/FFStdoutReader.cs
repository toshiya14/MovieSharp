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
        var buffer = new byte[1024 * 1024];

        while (offset < this.frameLength)
        {
            var rest = this.frameLength - offset;

            if (rest <= 0)
            {
                break;
            }

            if (rest > buffer.Length)
            {
                rest = buffer.Length;
            }
            else
            {
                buffer = new byte[rest];
            }

            this.stdout.ReadExactly(buffer);
            var span = frame.Slice(offset, rest);
            buffer.CopyTo(span);

            offset += rest;
        }

        return offset;
    }
}
