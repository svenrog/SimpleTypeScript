using BenchmarkDotNet.Attributes;

namespace SimpleTypeScript.Benchmarks;

/// <summary>
/// What the text itself costs, at a fixed size — a vocabulary module of five hundred entries, and a long
/// literal with and without anything to escape. Separate from the scaling benchmark beside it because none of
/// these varies with a declaration count, and running them once per size would only repeat a figure.
/// </summary>
[MemoryDiagnoser]
public class TextBenchmarks
{
    private readonly Dictionary<string, string> _entries = Entries(500);
    private readonly string _plain = new('a', 4_000);
    private readonly string _escaped = new('"', 4_000);

    /// <summary>A vocabulary module: one object literal with every key in it.</summary>
    [Benchmark]
    public string Vocabulary() =>
        new TsModule()
            .Const("MESSAGES", TsValue.Object(_entries), TsType.Record(TsType.String, TsType.String))
            .Render();

    [Benchmark(Baseline = true)]
    public string PlainText() => Render(_plain);

    /// <summary>The same length, every character needing an escape.</summary>
    [Benchmark]
    public string EscapedText() => Render(_escaped);

    private static string Render(string value) =>
        new TsModule().Const("X", TsValue.String(value)).Render(TsComment.Lines([]));

    private static Dictionary<string, string> Entries(int count) =>
        Enumerable
            .Range(0, count)
            .ToDictionary(index => $"section{index % 20}.key-{index}", index => $"Some text for key {index}.");
}
