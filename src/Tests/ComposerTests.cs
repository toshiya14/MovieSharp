using MovieSharp;
using MovieSharp.Composers;
using MovieSharp.Debugs.Benchmarks;
using MovieSharp.Objects;
using MovieSharp.Objects.Subtitles;
using MovieSharp.Tools;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Tests;

public class ComposerTest
{
    private string folder;
    private MediaFactory fac;
    private Logger log;

    [SetUp]
    public void Setup()
    {
        this.folder = AppDomain.CurrentDomain.BaseDirectory;
        this.fac = new MediaFactory();

        // log
        var logConfig = new LoggingConfiguration();
        var logTarget = new FileTarget("file") { FileName = Path.Combine(this.folder, "compose_tests.log") };
        var consoleTarget = new ColoredConsoleTarget("console");
        logConfig.AddTarget(logTarget);
        logConfig.AddTarget(consoleTarget);
        logConfig.AddRuleForAllLevels(logTarget);
        logConfig.AddRuleForAllLevels(consoleTarget);

        LogManager.Configuration = logConfig;
        this.log = LogManager.GetCurrentClassLogger();
    }

    [Test]
    public async Task Simple()
    {
        using var compose = this.fac.NewCompose(1920, 1080, 60);
        this.AddVideo(compose, false);
        this.AddSubtitle(compose);
        compose.OnFrameEncoded += (sender, e) => this.log.Info($"Finish: {e.Frame} @ {e.Speed}x / {e.Fps}fps");
        compose.OutputFile = Path.Combine(this.folder, "simple.mp4");
        compose.TempAudioFile = Path.Combine(this.folder, "audio.aac");
        //compose.RenderRange = compose.DetectMaxRange();
        compose.RenderRange = new TimeRange(0, 3.0f);

        //compose.ComposeAudio(new FFAudioParams());
        //compose.ComposeVideo();
        // ^ equals belows:
        compose.Compose();

        this.log.Info("Performance Report:\n" + PerformanceMeasurer.GetReport(TimeUnit.Milliseconds));
    }

    [Test]
    public async Task Complex()
    {
        using var compose = this.fac.NewCompose(1920, 1080, 60);
        this.AddVideo(compose, true);
        this.AddSubtitle(compose);
        compose.OnFrameEncoded += (sender, e) => this.log.Info($"Finish: {e.Frame} @ {e.Speed}x / {e.Fps}fps");
        compose.OutputFile = Path.Combine(this.folder, "complex.mp4");
        compose.TempAudioFile = Path.Combine(this.folder, "audio.aac");
        compose.RenderRange = new TimeRange(0, 10.0f);

        //compose.ComposeAudio(new FFAudioParams());
        //compose.ComposeVideo();
        // ^ equals belows:
        compose.Compose();

        this.log.Info("Performance Report:\n" + PerformanceMeasurer.GetReport(TimeUnit.Milliseconds));
    }

    [Test]
    public async Task StackTransformFilterCrop()
    {
        using var compose = this.fac.NewCompose(1920, 1080, 60);
        compose.PutVideo(0, this.fac.LoadVideo(Path.Combine(this.folder, "assets", "spring.mp4"))
                               .MakeClip()
                               .Crop(new RectBound(200, 200, 1920 - 400, 1080 - 400))
                               .Transform()
                               .AddTranslate(100, 100)
                               .AddScale(0.5f)
                               .ToClip()
        );
        compose.PutVideo(0, this.fac.LoadVideo(Path.Combine(this.folder, "assets", "summer.mp4"))
                               .MakeClip()
                               .Filter()
                               .AddBlur(10f, 10f)
                               .ToClip()
                               .Transform()
                               .AddTranslate(1920 / 2, 0)
                               .AddScale(0.5f)
                               .ToClip()
        );
        compose.PutVideo(0, this.fac.LoadVideo(Path.Combine(this.folder, "assets", "autumn.mp4"))
                               .MakeClip()
                               .Filter()
                               .AddPresetFilter(MovieSharp.Composers.Videos.PresetFilter.Sepia)
                               .ToClip()
                               .Transform()
                               .AddTranslate(0, 1080 / 2)
                               .AddScale(0.5f)
                               .ToClip()
        );
        var winter = this.fac.LoadVideo(Path.Combine(this.folder, "assets", "winter.mp4"));
        compose.PutVideo(1,
                               winter.MakeClip()
                               .Slice(1, 2)
                               .Transform()
                               .AddTranslate(1920 / 2, 1080 / 2)
                               .AddScale(0.5f)
                               .ToClip()
        );

        this.AddSubtitle(compose);
        compose.OnFrameEncoded += (sender, e) => this.log.Info($"Finish: {e.Frame} @ {e.Speed}x / {e.Fps}fps");
        compose.OutputFile = Path.Combine(this.folder, "stack-transform-filter-slice-crop-blur.mp4");
        compose.TempAudioFile = Path.Combine(this.folder, "audio.aac");
        compose.RenderRange = new TimeRange(0, 3.0f);
        //compose.UseMaxRenderRange();

        //compose.ComposeAudio(new FFAudioParams());
        //compose.ComposeVideo();
        // ^ equals belows:
        compose.Compose();

        this.log.Info("Performance Report:\n" + PerformanceMeasurer.GetReport(TimeUnit.Milliseconds));
    }

    [Test]
    public void RepeatSlice()
    {
        using var compose = this.fac.NewCompose(1920, 1080, 30);
        var spring = this.fac.LoadVideo(Path.Combine(this.folder, "assets", "spring.mp4"));
        compose.PutVideo(0, spring.MakeClip().Slice(0, 3).RepeatTimes(3));
        compose.OutputFile = Path.Combine(this.folder, "repeat-slice.mp4");
        compose.OnFrameEncoded += (sender, e) => this.log.Info($"Finish: {e.Frame} @ {e.Speed}x / {e.Fps}fps");
        compose.UseMaxRenderRange();
        compose.Compose();
    }

    [Test]
    public void AutoPaddingAudioLength() {
        using var compose = this.fac.NewCompose(1920, 1080, 30);
        var summer = this.fac.LoadVideo(Path.Combine(this.folder, "assets", "summer.mp4"));
        var bgm = this.fac.LoadAudio(Path.Combine(this.folder, "assets", "bgm.mp3"));
        compose.PutVideo(0, summer.MakeClip());
        compose.PutAudio(0, bgm.MakeClip());
        compose.OutputFile = Path.Combine(this.folder, "auto-pad-audio.mp4");
        compose.OnFrameEncoded += (sender, e) => this.log.Info($"Finish: {e.Frame} @ {e.Speed}x / {e.Fps}fps");
        compose.UseMaxRenderRange();
        compose.Compose();
    }


    private void AddVideo(ICompose compose, bool isBlur)
    {
        var summer = this.fac.LoadVideo(Path.Combine(this.folder, "assets", "summer.mp4")).MakeClip();
        if (isBlur)
        {
            summer = summer.Filter()
                     .AddBlur(10f, 10f)
                     .ToClip();
        }
        compose.PutVideo(0, summer);
    }

    private void AddSubtitle(ICompose compose)
    {
        var sub = this.fac.CreateSubtitle(1920, 1080);
        var stb = new SubtitleTimelineBuilder();
        var defaultFont = (FontDefinition font) =>
        {
            font.Family = new FontFamily(FontSource.System, "文泉驿微米黑");
            font.Size = 64;
            font.BorderColor = new RGBAColor(0x00, 0x00, 0x00, 0xff);
            font.Color = new RGBAColor(0xff, 0x00, 0x00, 0xff);
        };
        stb.AddSimple(1, 3, "测试字幕1")
           .WithTimeline(x =>
           {
               x.Anchor = Anchor.Center;
               x.TextAlign = TextAlign.Center;
           })
           .WithFont(defaultFont);
        stb.AddSimple(4, 6, "这是另外一个字幕")
           .WithTimeline(x =>
           {
               x.Anchor = Anchor.Center;
               x.TextAlign = TextAlign.Center;
           })
           .WithFont(defaultFont);
        sub.From(stb);
        var clip = sub.AsVideoSource().MakeClip();
        compose.PutVideo(0, clip);
    }
}