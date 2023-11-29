using MovieSharp.Objects;

namespace MovieSharp.Sources.Videos;

internal class DummyVideoSource : IVideoSource
{
    public long FrameCount => (long)(this.Duration * this.FrameRate);

    public double FrameRate { get; }

    public double Duration { get; }

    public Coordinate Size { get; }

    public PixelFormat PixelFormat { get; }

    private readonly RGBAColor? background;
    private Memory<byte>? buffer;

    public DummyVideoSource(RGBAColor? background, PixelFormat pixfmt, (int, int) size, double frameRate, double duration)
    {
        this.background = background;
        this.PixelFormat = pixfmt;
        this.Size = new Coordinate(size);
        this.FrameRate = frameRate;
        this.Duration = duration;
    }

    private Memory<byte>? MakeFrame()
    {
        if (this.background is null)
        {
            return null;
        }

        var bytesEachColor = this.PixelFormat.BitsEachColor / 8;
        var pixel = new byte[bytesEachColor];
        for (var i = 0; i < this.PixelFormat.ComponentsOrder.Length; i++)
        {
            var p = this.PixelFormat.ComponentsOrder[i];
            switch (p)
            {
                case 'r': pixel[i] = this.background.Red; break;
                case 'g': pixel[i] = this.background.Green; break;
                case 'b': pixel[i] = this.background.Blue; break;
                case 'a': pixel[i] = this.background.Alpha; break;
            }
        }

        var (w, h) = this.Size;
        var pixelsCount = w * h * bytesEachColor;
        var pixels = new byte[pixelsCount];
        for (var i = 0; i < pixelsCount; i += bytesEachColor)
        {
            for (var j = 0; j < bytesEachColor; j++)
            {
                pixels[i + j] = pixel[j];
            }
        }
        var buffer = pixels.AsMemory();
        this.buffer = buffer;
        return buffer;
    }

    public Memory<byte>? MakeFrame(long frameIndex)
    {
        return this.buffer ?? this.MakeFrame();
    }

    public Memory<byte>? MakeFrameByTime(double t)
    {
        return this.buffer ?? this.MakeFrame();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
