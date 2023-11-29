﻿using NLog;

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

    public Memory<byte>? ReadNextFrame()
    {
        var offset = 0;
        //var sw = Stopwatch.StartNew();
        var buffer = new byte[this.frameLength];

        while (offset < this.frameLength)
        {
            var r = this.stdout.Read(buffer, offset, this.frameLength - offset);
            if (r <= 0)
            {
                if (offset == 0) return null;
                else break;
            }
            offset += r;
        }

        if (offset != this.frameLength)
        {
            return null;
        }

        //sw.Stop();
        //log.Debug($"ReadNextFrame Finished: {sw.Elapsed}");

        return buffer.AsMemory();
    }
}
