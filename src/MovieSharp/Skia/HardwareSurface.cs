using SkiaSharp;

namespace MovieSharp.Skia;

internal enum HardwareBackend
{
    OpenGL,
    Vulkan
}

internal class HardwareSurface : IDisposable
{
    public HardwareSurface(SKImageInfo imageInfo, HardwareBackend backend = HardwareBackend.Vulkan)
    {
        this.Backend = backend;
        this.ImageInfo = imageInfo;
        this.Context = backend switch
        {
            HardwareBackend.Vulkan => GRContext.CreateVulkan(new GRVkBackendContext()),
            HardwareBackend.OpenGL => GRContext.CreateGl(),
            _ => throw new NotImplementedException("Does not support this kind of backend yet: " + backend),
        };
        this.Surface = SKSurface.Create(this.Context, true, this.ImageInfo);
    }

    public void Dispose()
    {
        this.Surface.Dispose();
    }

    public GRContext Context { get; }
    public HardwareBackend Backend { get; }
    public SKImageInfo ImageInfo { get; }
    public SKSurface Surface { get; }
}
