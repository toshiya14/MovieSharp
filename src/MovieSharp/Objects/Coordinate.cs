namespace MovieSharp.Objects;

public record Coordinate
{
    public int X { get; set; }
    public int Y { get; set; }
    public Coordinate(int x, int y)
    {
        this.X = x;
        this.Y = y;
    }

    public Coordinate((int, int) cord)
    {
        this.X = cord.Item1;
        this.Y = cord.Item2;
    }

    public void Deconstruct(out int x, out int y)
    {
        x = this.X;
        y = this.Y;
    }

    public Coordinate BothAdd(int movement)
    {
        return new Coordinate(this.X + movement, this.Y + movement);
    }

    public Coordinate BothMultiple(int factor)
    {
        return new Coordinate(this.X * factor, this.Y * factor);
    }

    public Coordinate Both(Func<int, int> calc)
    {
        return new Coordinate(calc(this.X), calc(this.Y));
    }
}
