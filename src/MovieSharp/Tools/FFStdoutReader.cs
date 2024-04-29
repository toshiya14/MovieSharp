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

    /// <summary>
    /// Read the next frame into memory. If return value < 0, means reading failed.
    /// </summary>
    /// <returns></returns>
    public int ReadNextFrame(Memory<byte> buffer)
    {
        if (this.stdout is null)
        {
            return -1;
        }

        if (buffer.Length < this.frameLength) {
            throw new ArgumentException($"ReadNextFrame needs the buffer be at least {this.frameLength} bytes, but got {buffer.Length} bytes.");
        }

        var cursor = 0;

        while (cursor < this.frameLength)
        {
            var span = buffer.Span[cursor..(this.frameLength - cursor)];
            var r = this.stdout.Read(span);
            if (r <= 0)
            {
                if (cursor == 0)
                {
                    return 0;
                }
                else
                {
                    cursor += r;
                    break;
                }
            }

            cursor += r;
        }

        // Adjust RawData length when changed
        //if (buffer.Length != cursor)
        //{
        //    return (buf.AsMemory()[..cursor], cursor);
        //}
        //else
        //{
        //    return (buf.AsMemory(), buf.Length);
        //}

        return cursor;
    }
}
