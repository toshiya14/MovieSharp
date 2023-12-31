﻿namespace MovieSharp.Composers.Videos;

public enum PresetFilter
{
    None,
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
