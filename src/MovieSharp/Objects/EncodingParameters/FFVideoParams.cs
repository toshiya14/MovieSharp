namespace MovieSharp.Objects.EncodingParameters;

public record FFVideoParams(
    Coordinate? Size = null,
    float? FrameRate = null,
    string Preset = "medium",
    string Codec = "libx264",
    string? Bitrate = null,
    int? CRF = 21,
    PixelFormat? SourcePixfmt = null,
    string TargetPixfmt = "yuv420p",
    string? WithCopyAudio = null,
    string FFMPEGBinary = "ffmpeg",
    RGBAColor? TransparentColor = null,
    int? Threads = null
)
{
    public Coordinate? Size { get; set; } = Size;
    public int? Threads { get; set; } = Threads ?? Environment.ProcessorCount;
    public float? FrameRate { get; set; } = FrameRate;
    public string? WithCopyAudio { get; set; } = WithCopyAudio;
    public PixelFormat SourcePixfmt { get; set; } = SourcePixfmt ?? PixelFormat.RGBA32;
    public RGBAColor TransparentColor { get; set; } = TransparentColor ?? RGBAColor.Black;
}
