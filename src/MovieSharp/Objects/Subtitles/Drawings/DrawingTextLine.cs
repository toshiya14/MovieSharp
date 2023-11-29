using SkiaSharp;

namespace MovieSharp.Objects.Subtitles.Drawings;

public class DrawingTextLine
{
    private DrawingTextBox parent;

    /// <summary>
    /// The line number.
    /// </summary>
    public int LineNumber { get; private set; }

    /// <summary>
    /// The text runs inside this line.
    /// </summary>
    public List<DrawingTextRun> Contents { get; private set; } = new List<DrawingTextRun>();

    /// <summary>
    /// The top offset for this line, related to the parent box.
    /// </summary>
    public float Top { get; private set; }

    /// <summary>
    /// The left offset for this line, related to the parent box.
    /// For each runs, the true left offset = this.left + run.left.
    /// </summary>
    public float Left { get; set; }

    /// <summary>
    /// The max height of the runs.
    /// </summary>
    public float MaxHeight { get; private set; }

    /// <summary>
    /// The spacing before this line.
    /// </summary>
    public float MarginTop { get; set; } = 0;

    public DrawingTextLine(DrawingTextBox parent, int linenum, float top, float marginTop = 0)
    {
        this.parent = parent;
        this.Top = top;
        this.MarginTop = marginTop;
        this.LineNumber = linenum;
    }

    /// <summary>
    /// Add content.
    /// </summary>
    /// <param name="content">The TextRun.</param>
    /// <param name="left">The left offset relatived to the line boundary box.</param>
    /// <param name="bound">The measured boundary.</param>
    /// <param name="trueWidth">Would use this instead of bound.Width if not null.</param>
    public void AddContent(TextRun content, float left, SKRect bound, float? trueWidth)
    {
        this.Contents.Add(new DrawingTextRun(this, content, left, -bound.Left, -bound.Top, trueWidth ?? bound.Width, bound.Height));
        if (this.MaxHeight < bound.Height)
        {
            this.MaxHeight = bound.Height;
        }
    }

    /// <summary>
    /// Enumerate the items inside.
    /// </summary>
    /// <returns>
    /// Item1 is TextRun, includes the texts; 
    /// Item2 is (float, float), represent the position to draw the text 
    /// (the position is based on the left top point of the text block).
    /// </returns>
    public IEnumerable<(DrawingTextRun, (float, float))> Enumerate()
    {
        for (var i = 0; i < this.Contents.Count; i++)
        {
            yield return (
                this.Contents[i],
                (
                    this.parent.Position.X + this.Left + this.Contents[i].Left,
                    this.parent.Position.Y + this.Top
                )
           );
        }
    }
}
