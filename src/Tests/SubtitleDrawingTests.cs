using System.Diagnostics;
using MovieSharp;
using MovieSharp.Objects;
using MovieSharp.Skia;
using MovieSharp.Tools;
using SkiaSharp;

namespace Tests;
public class SubtitleDrawingTests
{

    private string outputFolder;
    private bool writeResult = true;

    [SetUp]
    public void Setup()
    {
        this.outputFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skia_sub_data");
        Directory.CreateDirectory(this.outputFolder);
    }

    [Test]
    public void SimpleCenter()
    {
        var tb = new SubtitleTimelineBuilder();
        tb.AddSimple(0, 10, "Example")
          .WithTimeline(x =>
          {
              x.TextAlign = TextAlign.Center;
          })
          .WithFont(x =>
          {
              x.BorderColor = new RGBAColor(0x00, 0x00, 0x00, 0xff);
              x.Color = new RGBAColor(0xff, 0xff, 0xff, 0xff);
              x.Family = "Verdana";
              x.Size = 256f;
          });
        this.WorkWithTimeline(tb, "simple");
    }


    [Test]
    public void WrapTest()
    {
        var tb = new SubtitleTimelineBuilder();
        tb.AddSimple(0, 10, "A very long example to test the wrap.")
          .WithTimeline(x =>
          {
              x.TextAlign = TextAlign.Center;
          })
          .WithFont(x =>
          {
              x.BorderColor = new RGBAColor(0x00, 0x00, 0x00, 0xff);
              x.Color = new RGBAColor(0xff, 0xff, 0xff, 0xff);
              x.Family = "Verdana";
              x.Size = 180f;
          });

        this.WorkWithTimeline(tb, "wrap");
    }


    [Test]
    public void MultiRunAndWrapTest()
    {
        var tb = new SubtitleTimelineBuilder();
        var part1 = tb.AddComplex(0, 10).WithTimeline(x =>
        {
            x.TextAlign = TextAlign.Center;
            x.LineSpacing = 64f;
        });
        part1.UseFont(
              font =>
              {
                  font.BorderColor = new RGBAColor(0x00, 0x00, 0x00, 0xff);
                  font.Color = new RGBAColor(0xff, 0xff, 0xff, 0xff);
                  font.Family = "Verdana";
                  font.Size = 180f;
              })
             .AddRun("A very long ")
             .AddRun("example to test the wrap.");

        this.WorkWithTimeline(tb, "multi-wrap");
    }

    private void WorkWithTimeline(SubtitleTimelineBuilder stb, string saveFileName)
    {
        var fac = new MediaFactory();
        var sub = fac.CreateSubtitle(1920, 1080);
        sub.From(stb);
        var src = sub.AsVideoSource();

        using var img = new SKBitmap(1920, 1080);
        using var cvs = new SKCanvas(img);
        cvs.Clear(SKColors.AliceBlue);
        cvs.DrawImage(src.MakeFrame(0), 0, 0);

        var box = sub.GetLastTextBox();
        UnitTestUtils.PrintProperties(box);

        // Data Validating.
        foreach (var line in box!.Lines)
        {
            foreach (var (block, pos) in line.Enumerate())
            {
                var (x, y) = pos;
                var blockW = block.MeasuredWidth;
                var blockH = block.MeasuredHeight;
                if (this.writeResult)
                {
                    // draw boundary.
                    cvs.DrawRect(x, y, blockW, blockH, new SKPaint()
                    {
                        IsStroke = true,
                        Color = new SKColor(0x00, 0x00, 0xff),
                        StrokeWidth = 2,
                    });
                }
            }
        }
        if (this.writeResult)
        {
            var data = img.Encode(SKEncodedImageFormat.Png, 120);
            using var fs = File.OpenWrite(Path.Combine(this.outputFolder, $"{saveFileName}.png"));
            data.SaveTo(fs);

            Process.Start("explorer.exe", this.outputFolder);
        }
    }
}
