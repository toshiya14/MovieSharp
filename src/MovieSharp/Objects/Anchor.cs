namespace MovieSharp.Objects;

public class Anchor
{
    public float X { get; set; }
    public float Y { get; set; }

    public static Anchor LeftTop => new Anchor() { X = 0, Y = 0 };
    public static Anchor Top => new Anchor() { X = 0, Y = 0.5f };
    public static Anchor RightTop => new Anchor() { X = 0, Y = 1f };
    public static Anchor Left => new Anchor() { X = 0.5f, Y = 0 };
    public static Anchor Center => new Anchor() { X = 0.5f, Y = 0.5f };
    public static Anchor Right => new Anchor() { X = 0.5f, Y = 1f };
    public static Anchor LeftBottom => new Anchor() { X = 1f, Y = 0 };
    public static Anchor Bottom => new Anchor() { X = 1f, Y = 0.5f };
    public static Anchor RightBottom => new Anchor() { X = 1f, Y = 1f };

    public override string ToString()
    {
        return $"({this.X:0.00}, {this.Y:0.00})";
    }
}
