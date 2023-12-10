using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp;
public static class MovieSharpModuleConfig
{
    /// <summary>
    /// Set to `true`, would provide more trace logs and performance data.
    /// Set to `false`, would not contains any trace and performance data.
    /// </summary>
#if DEBUG
    public static bool RunInTestMode { get; set; } = true;
#else
    public static bool RunInTestMode { get; set; } = false;
#endif
}
