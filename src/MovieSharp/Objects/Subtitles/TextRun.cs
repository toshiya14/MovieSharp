namespace MovieSharp.Objects.Subtitles;

public record TextRun(string Text)
{
    public string Text { get; set; } = Text;
    public required FontDefinition Font { get; set; }

    /// <summary>
    /// Divide TextRun into 2 runs.
    /// </summary>
    /// <param name="index">The split position in Text.</param>
    /// <returns></returns>
    public (TextRun, TextRun) Fission(int index)
    {
        var left = this.Text[..index];
        var right = this.Text[index..];
        return (this with { Text = left }, this with { Text = right });
    }
}
