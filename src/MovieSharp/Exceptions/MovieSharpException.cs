using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp.Exceptions;

public enum MovieSharpErrorType
{
    RenderRangeOverflow
}

public class MovieSharpException : Exception
{

    private readonly Dictionary<string, object> data = new();

    public MovieSharpErrorType ErrorType { get; set; }
    public override IDictionary Data => data;


    public MovieSharpException(MovieSharpErrorType errorType, string? msg = null, Exception? inner = null) : base(msg, inner)
    {
        data["errorType"] = errorType;
        this.ErrorType = errorType;
    }

}
