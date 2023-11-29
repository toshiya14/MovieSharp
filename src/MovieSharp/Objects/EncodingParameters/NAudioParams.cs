namespace MovieSharp.Objects.EncodingParameters;

public record NAudioParams(
    string Codec = "aac",
    int? Resample = null,
    int Bitrate = 192000
)
{
}
