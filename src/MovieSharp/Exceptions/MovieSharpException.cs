using System.Collections;

namespace MovieSharp.Exceptions;

public enum MovieSharpErrorType
{
    RenderRangeOverflow,
    RenderRangeNotSet,
    ComposeNoClip,
    DrawingTimeGreaterThanDuration,
}

public class MovieSharpException : Exception
{

    private readonly Dictionary<string, object> data = new();

    public MovieSharpErrorType ErrorType { get; set; }
    public override IDictionary Data => this.data;


    public MovieSharpException(MovieSharpErrorType errorType, string? msg = null, Exception? inner = null) : base(msg, inner)
    {
        this.data["errorType"] = errorType;
        this.ErrorType = errorType;
    }

}
