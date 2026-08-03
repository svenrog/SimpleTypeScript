using BenchmarkDotNet.Attributes;

namespace SimpleTypeScript.Benchmarks;

/// <summary>
/// What writing a module costs. The sizes bracket a real generated file — a vocabulary is hundreds of
/// entries, a contract graph tens of interfaces — so a change of shape here is visible at the size it
/// matters at rather than at one chosen to look good.
/// </summary>
[MemoryDiagnoser]
public class EmitterBenchmarks
{
    [Params(10, 1000, 100_000)]
    public int Declarations { get; set; }

    [Benchmark]
    public string Interfaces()
    {
        var module = new TsModule();
        for (var index = 0; index < Declarations; index++)
        {
            module.Interface(
                $"Shape{index}",
                [
                    new TsMember("id", TsType.String, doc: "The identity."),
                    new TsMember("count", TsType.Number),
                    new TsMember("tags", TsType.ArrayOf(TsType.String)),
                    new TsMember("absent", TsType.Union([TsType.String, TsType.Null])),
                ],
                doc: "A shape.");
        }

        return module.Render();
    }
}
