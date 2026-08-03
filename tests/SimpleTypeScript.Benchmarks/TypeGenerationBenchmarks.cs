using BenchmarkDotNet.Attributes;
using SimpleTypeScript.TypeGeneration;
using SimpleTypeScript.TypeGeneration.Documentation;

namespace SimpleTypeScript.Benchmarks;

/// <summary>
/// What reading C# types costs, which is where a generator's time actually goes: reflection, nullability
/// metadata and — where it is asked for — an XmlDoc file per assembly.
/// </summary>
[MemoryDiagnoser]
public class TypeGenerationBenchmarks
{
    private static readonly Type[] _roots = [typeof(Order), typeof(Basket)];

    private readonly XmlDocumentationSource _documentation = new();

    [Benchmark(Baseline = true)]
    public string Walk() => Render(new TypeWalkerOptions());

    /// <summary>The same walk with doc comments, so the cost of carrying them is the difference.</summary>
    [Benchmark]
    public string WalkWithDocumentation() => Render(new TypeWalkerOptions { Documentation = _documentation });

    /// <summary>A lookup on an assembly already read, which is the case that repeats once per member.</summary>
    [Benchmark]
    public string? DocumentationLookup() => _documentation.For(typeof(Order));

    private static string Render(TypeWalkerOptions options)
    {
        var module = new TsModule();
        new TypeWalker(options).Add(_roots).Declare(module);

        return module.Render();
    }

    /// <summary>One order.</summary>
    public sealed class Order
    {
        /// <summary>Assigned when the order is placed.</summary>
        public Guid Id { get; init; }

        /// <summary>What the customer calls it.</summary>
        public string Reference { get; init; } = string.Empty;

        /// <summary>When it shipped, if it has.</summary>
        public DateTimeOffset? ShippedAt { get; init; }

        /// <summary>Where it is in its lifecycle.</summary>
        public Status Status { get; init; }

        /// <summary>What was ordered.</summary>
        public IReadOnlyList<Line> Lines { get; init; } = [];
    }

    /// <summary>One line of an order.</summary>
    public sealed class Line
    {
        /// <summary>How many.</summary>
        public int Quantity { get; init; }

        /// <summary>What it cost.</summary>
        public decimal Price { get; init; }

        /// <summary>What it is.</summary>
        public string Sku { get; init; } = string.Empty;
    }

    /// <summary>Lines held by region.</summary>
    public sealed class Basket
    {
        /// <summary>The lines, by the region they ship from.</summary>
        public IReadOnlyDictionary<string, Line> ByRegion { get; init; } = new Dictionary<string, Line>();
    }

    /// <summary>Where an order is in its lifecycle.</summary>
    public enum Status
    {
        /// <summary>Placed, not yet shipped.</summary>
        Open,

        /// <summary>Gone.</summary>
        Shipped,
    }
}
