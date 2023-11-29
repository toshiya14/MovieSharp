namespace MovieSharp.Objects;

public record struct RectBound(int Left, int Top, int Width, int Height)
{
    public readonly int Right => this.Left + this.Width;
    public readonly int Bottom => this.Top + this.Height;
}
