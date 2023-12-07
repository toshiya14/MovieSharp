namespace MovieSharp.Objects.EncodingParameters;

public record FFVideoParams(
    Coordinate? Size = null,
    float? FrameRate = null,
    string Preset = "medium",
    string Codec = "libx264",
    string? Bitrate = null,
    int? CRF = 21,
    string TargetPixfmt = "yuv420p",
    string? WithCopyAudio = null,
    RGBAColor? TransparentColor = null,
    int? Threads = null
)
{
    public Coordinate? Size { get; set; } = Size;
    public int? Threads { get; set; } = Threads ?? Environment.ProcessorCount / 4;
    public float? FrameRate { get; set; } = FrameRate;
    public string? WithCopyAudio { get; set; } = WithCopyAudio;
    public RGBAColor TransparentColor { get; set; } = TransparentColor ?? RGBAColor.Black;
}
