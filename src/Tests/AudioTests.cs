using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MovieSharp;
using NLog.Config;
using NLog.Targets;
using NLog;
using NUnit.Framework;
using MovieSharp.Composers;

namespace Tests;
public class AudioTests
{
    private string folder;
    private string outputFolder;
    private MediaFactory fac;
    private Logger log;

    [SetUp]
    public void Setup()
    {

        this.folder = AppDomain.CurrentDomain.BaseDirectory;
        this.outputFolder = Path.Join(this.folder, "test_out");
        this.fac = new MediaFactory();

        // log
        var logConfig = new LoggingConfiguration();
        var logTarget = new FileTarget("file") { FileName = Path.Combine(this.outputFolder, "compose_tests.log") };
        var consoleTarget = new ColoredConsoleTarget("console");
        logConfig.AddTarget(logTarget);
        logConfig.AddTarget(consoleTarget);
        logConfig.AddRuleForAllLevels(logTarget);
        logConfig.AddRuleForAllLevels(consoleTarget);

        LogManager.Configuration = logConfig;
        this.log = LogManager.GetCurrentClassLogger();
    }

    [Test]

    public void TestRepeat()
    {
        var aud = this.fac.LoadAudio(Path.Combine(this.folder, "assets", "bgm.mp3"));
        var clip = aud.MakeClip();
        clip = clip.RepeatTo(600);

        clip.ToFile(Path.Combine(this.outputFolder, "bgm.aac"), new MovieSharp.Objects.EncodingParameters.NAudioParams { Bitrate = 192000, Codec = "aac", Resample = 44100 });
    }
}
