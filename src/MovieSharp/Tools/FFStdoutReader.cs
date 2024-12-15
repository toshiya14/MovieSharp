using System.Drawing;
using NLog;

namespace MovieSharp.Tools;

internal class FFStdoutReader
{
    private readonly Stream stdout;
    private readonly int frameLength;

    public FFStdoutReader(Stream stdout, int frameLength)
    {
        this.stdout = stdout;
        this.frameLength = frameLength;
    }

    public int ReadNextFrame(Memory<byte> frame) {
        if (frame.Length != this.frameLength) {
            throw new Exception($"LOGIC ERROR: Could not read frame, the length of the memory provided({frame.Length}) not as same as frameLength({this.frameLength}).");
        }
        this.stdout.ReadExactly(frame.Span);
        return this.frameLength;
    }
}
