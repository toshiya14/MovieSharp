using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp.Composers.Videos;

public enum PresetFilter { 
    Retro,
    Sepia,
    Nostalgic,
    Polaroid,
    SwapRG,
    SwapRB,
    SwapGB
}

public interface IFilteredVideoClip
{
    IFilteredVideoClip AddBlur(float sigmaX, float sigmaY);
    IFilteredVideoClip AddColorTempOffset(float offset);
    IFilteredVideoClip AddPresetFilter(PresetFilter preset);
    IVideoClip ToClip();
}
