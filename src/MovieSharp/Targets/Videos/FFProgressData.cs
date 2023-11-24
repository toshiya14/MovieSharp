using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp.Targets.Videos;

public enum FFProgressState { 
    Continue,
    End
}

public record FFProgressData
{
    /// <summary>
    /// The progress state.
    /// </summary>
    public FFProgressState Progress { get; set; }

    /// <summary>
    /// Current working frame index.
    /// </summary>
    public long Frame { get; set; }

    /// <summary>
    /// Current encoding speed, unit: frame(s) per seconds.
    /// </summary>
    public float Fps { get; set; }

    /// <summary>
    /// Overall video bitrate before the working frame.
    /// </summary>
    public string Bitrate { get; set; } = string.Empty;

    /// <summary>
    /// Current encoding speed, 1x means 1 second in video and 1 second in real time.
    /// </summary>
    public float Speed { get; set; }

    /// <summary>
    /// Overall video size before the working frame.
    /// </summary>
    public long TotalSize { get; set; }
}
