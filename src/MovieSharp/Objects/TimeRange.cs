namespace MovieSharp.Objects;

public struct TimeRange
{
    public double Left { get; set; }
    public double Right { get; set; }

    public TimeRange(double left, double right)
    {
        this.Left = left;
        this.Right = right;
    }
}
