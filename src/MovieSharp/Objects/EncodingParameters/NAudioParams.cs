using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp.Objects.EncodingParameters;

public record NAudioParams(
    string Codec = "aac",
    int? Resample = null,
    int Bitrate = 192000
)
{
}
