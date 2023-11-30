﻿using MovieSharp.Composers.Audios;
using NAudio.Wave;

namespace MovieSharp.Composers;

public interface IAudioClip : IDisposable
{
    double Duration { get; }

    int Channels { get; }

    int SampleRate { get; }

    ISampleProvider? GetSampler();
}

public static class IAudioClipExtensions
{
    public static IAudioClip MakeClip(this IAudioSource source)
    {
        return new AudioSourceClip(source);
    }

    public static IAudioClip ToMono(this IAudioClip clip)
    {
        return new ChannelExtensionAudioClipProxy(clip, 1);
    }

    public static IAudioClip ToStereo(this IAudioClip clip)
    {
        return new ChannelExtensionAudioClipProxy(clip, 2);
    }

    public static IAudioClip ChangeChannels(this IAudioClip clip, int channels)
    {
        return new ChannelExtensionAudioClipProxy(clip, channels);
    }

    public static IAudioClip GainVolume(this IAudioClip clip, float multipler)
    {
        return new GainedAudioClipProxy(clip, multipler);
    }

    public static IAudioClip Resample(this IAudioClip clip, int samplerate)
    {
        return new ResampleAudioClipProxy(clip, samplerate);
    }

    public static IAudioClip Slice(this IAudioClip clip, double start, double end)
    {
        return new SlicedAudioClipProxy(clip, start, end);
    }

    public static IAudioClip RepeatTo(this IAudioClip clip, double newDuration)
    {
        return new RepeatAudioClipProxy(clip, newDuration);
    }

    public static IAudioClip RepeatTimes(this IAudioClip clip, int times)
    {
        return new RepeatAudioClipProxy(clip, times * clip.Duration);
    }

    public static IAudioClip FollowedBy(this IAudioClip clip, IAudioClip other)
    {
        return new ConcatenatedAudioClipProxy(clip, other);
    }
}