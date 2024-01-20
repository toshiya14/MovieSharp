using IronSoftware.Drawing;

namespace MovieSharp.Objects.Subtitles.Drawings;

public class DrawingTextRun
{
    /// <summary>
    /// The left offset for this run, related to this line.
    /// </summary>
    public float Left { get; set; }

    /// <summary>
    /// The ascent for this run.
    /// </summary>
    public float Ascent { get; set; }

    /// <summary>
    /// The leading space for this run.
    /// </summary>
    public float LeadingSpace { get; set; }

    /// <summary>
    /// The width measured by SKPaint.
    /// </summary>
    public float MeasuredWidth { get; set; }

    /// <summary>
    /// The height measured by SKPaint.
    /// </summary>
    public float MeasuredHeight { get; set; }

    /// <summary>
    /// The DrawingTextLine belongs to.
    /// </summary>
    private DrawingTextLine parent { get; set; }

    /// <summary>
    /// The text.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// The font.
    /// </summary>
    public FontDefinition Font { get; set; }

    public DrawingTextRun(DrawingTextLine parent, TextRun text, float left, float leading, float ascent, float measuredWidth, float measuredHeight)
    {
        this.parent = parent;
        this.Text = text.Text;
        this.Font = text.Font;
        this.Left = left;
        this.LeadingSpace = leading;
        this.Ascent = ascent;
        this.MeasuredWidth = measuredWidth;
        this.MeasuredHeight = measuredHeight;
    }

    public int GetLineNumber()
    {
        return this.parent.LineNumber;
    }

}
