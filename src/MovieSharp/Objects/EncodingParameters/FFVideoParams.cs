using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    string FFMPEGBinary = "ffmpeg"
)
{
    public Coordinate? Size { get; set; } = Size;
    public float? FrameRate { get; set; } = FrameRate;
    public string? WithCopyAudio { get; set; } = WithCopyAudio;
    public PixelFormat SourcePixfmt { get; set; } = SourcePixfmt ?? PixelFormat.RGBA32;
}
