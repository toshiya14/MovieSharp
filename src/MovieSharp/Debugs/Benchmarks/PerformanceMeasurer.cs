using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp.Debugs.Benchmarks;

internal class MeasurerContext : IDisposable
{
    private PerformanceMeasurer measurer;
    private string name;

    public MeasurerContext(PerformanceMeasurer measurer, string name)
    {
        this.measurer = measurer;
        this.name = name;
    }

    public void Dispose()
    {
        this.measurer.StopMeasure(this.name);
    }
}

public enum TimeUnit
{
    Ticks,
    Milliseconds
}

public class PerformanceMeasurer
{

    static Dictionary<string, Dictionary<string, IList<long>>> MeasuredData { get; set; } = new();

    internal string ClassName { get; }
    private Dictionary<string, IList<long>> CurrentMesurement =>
        MeasuredData[this.ClassName];
    private Dictionary<string, DateTime> BasedTimes { get; set; } = new();

    internal PerformanceMeasurer(string className, bool clearIfExists = false)
    {
        this.ClassName = className;
        if (!MeasuredData.ContainsKey(className) || clearIfExists)
        {
            MeasuredData[className] = new();
        }
    }

    internal static PerformanceMeasurer GetCurrentClassMeasurer()
    {
        var st = new StackTrace(new StackFrame(1));
        var sf = st.GetFrame(0);

        return new PerformanceMeasurer(sf?.GetMethod()?.DeclaringType?.FullName ?? sf?.GetMethod()?.Name ?? string.Empty);
    }

    internal void BeginMeasure(string name, int capacity = 12)
    {
        if (!this.CurrentMesurement.ContainsKey(name))
        {
            this.CurrentMesurement[name] = new List<long>(capacity);
        }

        this.BasedTimes[name] = DateTime.UtcNow;
    }

    internal void StopMeasure(string name)
    {
        var delta = DateTime.UtcNow - this.BasedTimes[name];
        this.CurrentMesurement[name].Add(delta.Ticks);
    }

    internal MeasurerContext UseMeasurer(string name, int capacity = 12)
    {
        this.BeginMeasure(name, capacity);
        return new MeasurerContext(this, name);
    }

    public static string GetReport(TimeUnit unit = TimeUnit.Ticks)
    {
        var sb = new StringBuilder();
        foreach (var (c, cls) in MeasuredData)
        {
            if (cls.Any())
            {
                var maxMeasureNameLength = cls.Select(x => x.Key.Length).Max();
                sb.AppendLine($"Class {c}:");
                foreach (var (key, measurements) in cls)
                {
                    var ordered = measurements.OrderBy(x => x);
                    var length = measurements.Count;
                    var p90i = (int)(length * 0.9);
                    var p95i = (int)(length * 0.95);
                    var p99i = (int)(length * 0.99);
                    // measured times:
                    var factor =
                        unit is TimeUnit.Ticks ? 1.0
                        : unit is TimeUnit.Milliseconds ? (double)TimeSpan.TicksPerMillisecond
                        : throw new ArgumentException("Unknown TimeUnit: " + unit);
                    var max = measurements.Max() / factor;
                    var min = measurements.Min() / factor;
                    var avg = measurements.Average() / factor;
                    var p90 = ordered.ElementAt(p90i) / factor;
                    var p95 = ordered.ElementAt(p95i) / factor;
                    var p99 = ordered.ElementAt(p99i) / factor;
                    // output
                    sb.AppendLine($"  ({measurements.Count.ToString().PadLeft(5, ' ')}) {key.PadRight(maxMeasureNameLength)} | Max:{max:0.00} Min: {min:0.00} Avg:{avg:0.00} 90%:{p90:0.00} 95%:{p95:0.00} 99%:{p99:0.00}");
                }
            }
        }
        return sb.ToString();
    }
}
