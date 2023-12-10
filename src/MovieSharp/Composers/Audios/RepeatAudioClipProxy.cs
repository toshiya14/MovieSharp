using NAudio.Wave;

namespace MovieSharp.Composers.Audios;
internal class RepeatAudioClipProxy : IAudioClip
{
    private readonly IAudioClip baseclip;

    public double Duration { get; }

    public int Channels => this.baseclip.Channels;

    public int SampleRate => this.baseclip.SampleRate;

    public RepeatAudioClipProxy(IAudioClip baseclip, double duration)
    {
        this.baseclip = baseclip;
        this.Duration = duration;
    }

    public ISampleProvider? GetSampler()
    {
        var sampler = this.baseclip.GetSampler();
        if (sampler == null)
        {
            return null;
        }

        var restTime = this.Duration - this.baseclip.Duration;
        while (restTime > 0)
        {
            if (restTime >= this.baseclip.Duration)
            {
                sampler = sampler.FollowedBy(this.baseclip.GetSampler());
                restTime -= this.baseclip.Duration;
            }
            else
            {
                sampler = sampler.FollowedBy(
                    this.baseclip.GetSampler().Take(TimeSpan.FromSeconds(restTime))
                );
                restTime -= restTime;
            }
        }
        return sampler;
    }

    public void Dispose()
    {
        this.baseclip.Dispose();
        GC.SuppressFinalize(this);
    }
}
