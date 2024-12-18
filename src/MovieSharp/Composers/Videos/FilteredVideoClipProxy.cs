using MovieSharp.Debugs.Benchmarks;
using MovieSharp.Objects;
using MovieSharp.Skia.Surfaces;
using SkiaSharp;

namespace MovieSharp.Composers.Videos;

internal enum VideoFilterType { 
    None,
    Blur,
    ColorTempOffset,
    PresetFilter
}

internal record VideoFilterRule(
    VideoFilterType Type
) {
    internal record Blur(float SigmaX, float SigmaY) : VideoFilterRule(VideoFilterType.Blur);
    internal record ColorTempOffset(float Offset): VideoFilterRule(VideoFilterType.ColorTempOffset);
    internal record Preset(PresetFilter Filter): VideoFilterRule(VideoFilterType.PresetFilter);
}

internal class FilteredVideoClipProxy : IVideoClip, IFilteredVideoClip
{
    private readonly IVideoClip baseclip;

    private readonly List<VideoFilterRule> rules = new();

    public Coordinate Size => this.baseclip.Size;

    public double Duration => this.baseclip.Duration;

    public FilteredVideoClipProxy(IVideoClip baseclip)
    {
        this.baseclip = baseclip;
    }

    public void Dispose()
    {
        this.baseclip.Dispose();
    }

    public IVideoClip ToClip()
    {
        return this;
    }

    public void Draw(SKCanvas canvas, SKPaint? paint, double time)
    {
        using var _ = PerformanceMeasurer.UseMeasurer("filtered-drawing");

        SKPaint _paint;
        var disposePaint = false;
        if (paint is null) {
            _paint = new SKPaint() { IsAntialias = true };
            disposePaint = true;
        } else {
            _paint = paint;
        }

        ApplyRules(_paint, this.rules);

        this.baseclip.Draw(canvas, paint, time);

        if (disposePaint) {
            _paint.Dispose();
        }
    }

    public IFilteredVideoClip AddBlur(float sigmaX, float sigmaY)
    {
        this.rules.Add(new VideoFilterRule.Blur(sigmaX, sigmaY));
        return this;
    }

    public IFilteredVideoClip AddColorTempOffset(float offset)
    {
        this.rules.Add(new VideoFilterRule.ColorTempOffset(offset));
        return this;
    }

    public IFilteredVideoClip AddPresetFilter(PresetFilter filter)
    {
        this.rules.Add(new VideoFilterRule.Preset(filter));
        return this;
    }

    private static void ApplyRules(SKPaint paint, List<VideoFilterRule> rules) {
        foreach (var rule in rules) {
            switch (rule) {
                case VideoFilterRule.Blur blur:
                    AddBlur(paint, blur.SigmaX, blur.SigmaY);
                    break;

                case VideoFilterRule.ColorTempOffset colortemp:
                    AddColorTempOffset(paint, colortemp.Offset);
                    break;

                case VideoFilterRule.Preset preset:
                    AddPresetFilter(paint, preset.Filter);
                    break;
            }
        }
    }

    private static void AddBlur(SKPaint paint, float sigmaX, float sigmaY)
    {
        if (paint.ImageFilter is null)
        {
            paint.ImageFilter = SKImageFilter.CreateBlur(sigmaX, sigmaY);
        }
        else
        {
            paint.ImageFilter = SKImageFilter.CreateMerge(
                paint.ImageFilter,
                SKImageFilter.CreateBlur(sigmaX, sigmaY)
            );
        }
    }

    private static void AddColorTempOffset(SKPaint paint, float offset)
    {
        float[] matrix;
        if (offset < 0)
        {
            matrix = new float[] {
                1f - (0.39f * -offset), 0, 0, 0, 0,
                0, 1, 0, 0, 0,
                0, 0, 1, 0, 0,
                0, 0, 0, 1, 0
            };
        }
        else if (offset > 0)
        {
            matrix = new float[] {
                1, 0, 0, 0, 0,
                0, 1 - (0.08f * offset), 0, 0, 0,
                0, 0, 1 - (0.19f * offset), 0, 0,
                0, 0, 0, 1, 0
            };
        }
        else
        {
            // Offset = 0, no colors would be changed.
            return;
        }
        AppendColorFilter(paint, matrix);
    }

    private static void AddPresetFilter(SKPaint paint, PresetFilter preset)
    {
        var matrix = preset switch
        {
            PresetFilter.Retro => new float[]
            {
                0.5f, 0.5f, 0.5f, 0, 0,
                1 / 3f, 1 / 3f, 1 / 3f, 0, 0,
                0.25f, 0.25f, 0.25f, 0, 0,
                0, 0, 0, 1, 0
            },
            PresetFilter.Sepia => new float[]
            {
                0.3588f, 0.7044f, 0.1368f, 0, 0,
                0.2990f, 0.5870f, 0.1140f, 0, 0,
                0.2392f, 0.4696f, 0.0912f, 0, 0,
                0, 0, 0, 1, 0
            },
            PresetFilter.Nostalgic => new float[]
            {
                0.272f, 0.534f, 0.131f, 0, 0,
                0.349f, 0.686f, 0.168f, 0, 0,
                0.393f, 0.769f, 0.189f, 0, 0,
                0, 0, 0, 1, 0,
            },
            PresetFilter.Polaroid => new float[]
            {
                1.438f, -0.062f, -0.062f, 0, 0,
                -0.122f, 1.378f, -0.122f, 0, 0,
                -0.016f, -0.016f, 1.483f, 0, 0,
                -0.03f, 0.05f, 0.02f, 1, 0
            },
            PresetFilter.SwapRG => new float[]
            {
                0, 1, 0, 0, 0,
                1, 0, 0, 0, 0,
                0, 0, 1, 0, 0,
                0, 0, 0, 1, 0
            },
            PresetFilter.SwapRB => new float[]
            {
                0, 0, 1, 0, 0,
                0, 1, 0, 0, 0,
                1, 0, 0, 0, 0,
                0, 0, 0, 1, 0
            },
            PresetFilter.SwapGB => new float[]
            {
                1, 0, 0, 0, 0,
                0, 0, 1, 0, 0,
                0, 1, 0, 0, 0,
                0, 0, 0, 1, 0,
            },
            _ => throw new NotSupportedException("This preset of Filter has not been supported yet: " + preset),
        };
        AppendColorFilter(paint, matrix);
    }

    private static void AppendColorFilter(SKPaint paint, float[] matrix)
    {
        var filter = SKColorFilter.CreateColorMatrix(matrix);
        if (paint.ColorFilter is null)
        {
            paint.ColorFilter = filter;
        }
        else
        {
            paint.ColorFilter = SKColorFilter.CreateCompose(
                filter,
                paint.ColorFilter
            );
        }
    }

    public void Release()
    {
        this.baseclip.Release();
    }
}
