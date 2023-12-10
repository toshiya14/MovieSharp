//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading.Tasks;
//using CryMediaAPI.Video;
//using MovieSharp.Debugs.Benchmarks;
//using MovieSharp.Exceptions;
//using MovieSharp.Objects;
//using NLog;
//using SkiaSharp;

//namespace MovieSharp.Sources.Videos;
//internal class CryMediaVideoSource : IVideoSource
//{
//    private readonly ILogger log = LogManager.GetCurrentClassLogger();

//    #region Const
//    public const int ReloadThreadFrames = 100;
//    #endregion

//    #region Auto Properties
//    public int FrameCount => this.VideoReader.LoadedMetadata ? this.VideoReader.Metadata.PredictedFrameCount : throw new MovieSharpException(MovieSharpErrorType.VideoSourceNotInitialized, "CryMediaVideoSource has not been initialized.");
//    public double FrameRate => this.VideoReader.LoadedMetadata ? this.VideoReader.Metadata.AvgFramerate : throw new MovieSharpException(MovieSharpErrorType.VideoSourceNotInitialized, "CryMediaVideoSource has not been initialized.");
//    public double Duration => this.VideoReader.LoadedMetadata ? this.VideoReader.Metadata.Duration : throw new MovieSharpException(MovieSharpErrorType.VideoSourceNotInitialized, "CryMediaVideoSource has not been initialized.");
//    public Coordinate Size => this.VideoReader.LoadedMetadata ? new Coordinate(this.VideoReader.Metadata.Width, this.VideoReader.Metadata.Height) : throw new MovieSharpException(MovieSharpErrorType.VideoSourceNotInitialized, "CryMediaVideoSource has not been initialized.");
//    public PixelFormat PixelFormat => PixelFormat.RGBA32;
//    public int Position => (int)this.VideoReader.CurrentFrameOffset;
//    #endregion

//    #region Properties
//    public string FilePath { get; }
//    public string FFMPEGBinary { get; }
//    public string FFPROBEBinary { get; }
//    public VideoReader VideoReader { get; private set; }
//    public SKImageInfo ImageInfo { get; private set; }
//    #endregion

//    #region Intializer
//    public CryMediaVideoSource(string filepath, string ffmpegBin = "ffmpeg", string ffprobeBin = "ffprobe")
//    {
//        this.FilePath = filepath;
//        this.FFMPEGBinary = ffmpegBin;
//        this.FFPROBEBinary = ffprobeBin;
//        this.VideoReader = new VideoReader(this.FilePath, this.FFMPEGBinary, this.FFPROBEBinary);
//    }

//    public void Init(int frameIndex)
//    {
//        using var _ = PerformanceMeasurer.UseMeasurer("init");

//        this.log.Info($"Initializing {this.FilePath} @ {frameIndex}.");

//        this.VideoReader?.Dispose();
//        this.VideoReader = new VideoReader(this.FilePath, this.FFMPEGBinary, this.FFPROBEBinary);

//        if (!this.VideoReader.LoadedMetadata)
//        {
//            this.VideoReader.LoadMetadata();
//        }

//        this.ImageInfo = new SKImageInfo(this.Size.X, this.Size.Y, SKColorType.Rgba8888, SKAlphaType.Unpremul);

//        if (frameIndex > 0)
//        {
//            var startTime = this.GetTimeByFrameId(frameIndex);
//            this.VideoReader.Load(startTime);
//        }
//        else
//        {
//            this.VideoReader.Load(0);
//        }
//    }
//    #endregion

//    #region Private Methods
//    public void Seek(int frameIndex)
//    {
//        if (frameIndex < this.Position || frameIndex - this.Position > ReloadThreadFrames)
//        {
//            this.log.Trace($"re-initialize: {frameIndex}");
//            this.Init(frameIndex);
//        }
//        else
//        {
//            using (PerformanceMeasurer.UseMeasurer("skip-frames"))
//            {
//                var list = new List<int>();
//                while (this.Position < frameIndex)
//                {
//                    list.Add(this.Position);
//                    this.VideoReader.NextFrame();
//                }

//                this.log.Trace($"Skipped frames: {string.Join(',', list)}");
//            }
//        }
//    }
//    #endregion

//    #region Public Methods
//    public double GetTimeByFrameId(int frameIndex)
//    {
//        return frameIndex / this.FrameRate;
//    }

//    public int GetFrameId(double time)
//    {
//        return (int)(time * this.FrameRate);
//    }

//    public void Dispose()
//    {
//        this.VideoReader?.Dispose();
//    }


//    public SKBitmap? MakeFrameById(int frameId)
//    {
//        using var _0 = PerformanceMeasurer.UseMeasurer("make-frame-total");

//        this.log.Info($"Make frame for {frameId}, current position: {this.Position}");
//        this.Seek(frameId);

//        using var _1 = PerformanceMeasurer.UseMeasurer("make-frame");

//        var frame = this.VideoReader.NextFrame();
//        var oi = 0;
//        var ni = 0;
//        var components = 0;
//        var newFrame = new byte[frame.Width * frame.Height * 4];
//        foreach (var bytedata in frame.RawData.Span)
//        {
//            newFrame[ni++] = frame.RawData.Span[oi++];

//            if (components == 2)
//            {
//                components = 0;
//                newFrame[ni++] = 0xff;
//            }
//            else
//            {
//                components++;
//            }
//        }

//        var framePinned = GCHandle.Alloc(newFrame, GCHandleType.Pinned);
//        var bitmap = new SKBitmap(this.ImageInfo);
//        bitmap.InstallPixels(this.ImageInfo, framePinned.AddrOfPinnedObject(), this.ImageInfo.RowBytes, delegate { framePinned.Free(); });
//        return bitmap;
//    }

//    public SKBitmap? MakeFrameByTime(double time) => this.MakeFrameById(this.GetFrameId(time));

//    #endregion
//}
