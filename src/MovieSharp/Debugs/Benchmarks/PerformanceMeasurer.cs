using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MovieSharp.Debugs.Benchmarks;

internal class MeasurerContext : IDisposable
{
    private string name;

    public MeasurerContext(string name)
    {
        this.name = name;
    }

    public void Dispose()
    {
        PerformanceMeasurer.StopMeasure(this.name);
    }
}

public enum TimeUnit
{
    Ticks,
    Milliseconds
}

public static class PerformanceMeasurer
{

    static Dictionary<string, IList<long>> MeasuredData { get; set; } = new();

    static Dictionary<string, DateTime> BasedTimes { get; set; } = new();

    internal static void BeginMeasure(string name)
    {
        if (MovieSharpModuleConfig.RunInTestMode)
        {
            return;
        }
        if (!MeasuredData.ContainsKey(name))
        {
            MeasuredData[name] = new List<long>();
        }
        BasedTimes[name] = DateTime.UtcNow;
    }

    internal static void StopMeasure(string name)
    {
        if (MovieSharpModuleConfig.RunInTestMode)
        {
            return;
        }
        var delta = DateTime.UtcNow - BasedTimes[name];
        MeasuredData[name].Add(delta.Ticks);
    }

    internal static MeasurerContext UseMeasurer(string name)
    {
        BeginMeasure(name);
        return new MeasurerContext(name);
    }

    public static string GetReport(TimeUnit unit = TimeUnit.Ticks, bool writeToFile = true)
    {
        if (MovieSharpModuleConfig.RunInTestMode)
        {
            return "Not in test mode.";
        }
        var sb = new StringBuilder();
        var maxMeasureNameLength = MeasuredData.Select(x => x.Key.Length).Max();
        foreach (var (c, data) in MeasuredData)
        {
            if (data.Any())
            {
                var ordered = data.OrderBy(x => x);
                var length = data.Count;
                var p90i = (int)(length * 0.9);
                var p95i = (int)(length * 0.95);
                var p99i = (int)(length * 0.99);
                // measured times:
                var factor =
                    unit is TimeUnit.Ticks ? 1.0
                    : unit is TimeUnit.Milliseconds ? (double)TimeSpan.TicksPerMillisecond
                    : throw new ArgumentException("Unknown TimeUnit: " + unit);
                var max = data.Max() / factor;
                var min = data.Min() / factor;
                var avg = data.Average() / factor;
                var p90 = ordered.ElementAt(p90i) / factor;
                var p95 = ordered.ElementAt(p95i) / factor;
                var p99 = ordered.ElementAt(p99i) / factor;
                // output
                sb.AppendLine($"  ({data.Count.ToString().PadLeft(5, ' ')}) {c.PadRight(maxMeasureNameLength)} | Max:{max:0.00} Min: {min:0.00} Avg:{avg:0.00} 90%:{p90:0.00} 95%:{p95:0.00} 99%:{p99:0.00}");

            }
        }

        if (writeToFile)
        {
            var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reports");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, $"Performance-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.json");
            File.WriteAllText(path, JsonSerializer.Serialize(MeasuredData));
        }

        return sb.ToString();
    }
}
