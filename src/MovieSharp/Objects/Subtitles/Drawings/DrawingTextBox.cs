namespace MovieSharp.Objects.Subtitles.Drawings;

public class DrawingTextBox
{
    /// <summary>
    /// The position of left top point of the TextBox.
    /// </summary>
    public Coordinate Position { get; set; } = new Coordinate(0, 0);

    /// <summary>
    /// The size of the TextBox, generated during measuring.
    /// </summary>
    public Coordinate BoxSize { get; set; } = new Coordinate(0, 0);

    /// <summary>
    /// Each lines.
    /// </summary>
    public IList<DrawingTextLine> Lines { get; set; } = new List<DrawingTextLine>();

    /// <summary>
    /// The gaps between each lines.
    /// </summary>
    public float LinesGap { get; set; } = 0;
}
