using SimpleTypeScript.TypeGeneration.Documentation;
using Xunit;

namespace SimpleTypeScript.Tests.Performance;

/// <summary>
/// What a call costs, held to a ceiling.
/// <para>
/// Ceilings rather than exact figures: an allocation count moves with the framework, and a test asserting the
/// current one to the byte fails on a runtime upgrade for no reason anybody cares about. Each is set well
/// above what the call actually allocates, so it catches a change of <em>shape</em> — a string built per
/// character, a file re-read per lookup, a LINQ chain on a path that repeats — and nothing smaller.
/// </para>
/// </summary>
public sealed class AllocationTests
{
    private static readonly XmlDocumentationSource _documentation = new();

    private static readonly string _long = new('a', 4_000);

    /// <summary>
    /// A value is written into the module's own buffer. What a render costs is its text, and the ceiling is
    /// above one copy of it and below the several a per-character string would make.
    /// </summary>
    [Fact]
    public void Rendering_a_value_costs_its_text_and_a_buffer_to_hold_it()
    {
        var module = new TsModule().Const("X", TsValue.String(_long));

        Under(64_000, "a 4,000-character literal", () => module.Render(TsComment.Empty()));
    }

    /// <summary>
    /// Escaping walks the string once and appends what it reads. The failure worth catching is a rewrite per
    /// character, which is what a naive `Replace` chain would be.
    /// </summary>
    [Fact]
    public void Escaping_walks_the_string_once()
    {
        var value = TsValue.String(_long);
        var module = new TsModule().Const("X", value);

        var plain = Allocation.PerCall(() => module.Render(TsComment.Empty()));
        var escaped = Allocation.PerCall(() =>
            new TsModule().Const("X", TsValue.String(_long.Replace('a', '"'))).Render(TsComment.Empty()));

        // Escaping every character doubles the text and nothing else; anything much beyond that is a copy
        // per escape rather than one pass.
        Assert.True(
            escaped < plain * 6,
            $"a fully escaped string costs {(double)escaped / plain:0.0}x a plain one of the same length");
    }

    /// <summary>
    /// The documentation file is parsed once per assembly. A lookup that re-read it would be invisible on a
    /// small type and would dominate a walk over a real contract graph.
    /// </summary>
    [Fact]
    public void A_documentation_lookup_reads_the_file_once()
    {
        _documentation.For(typeof(AllocationTests));

        Under(1_000, "a repeated type lookup", () => _documentation.For(typeof(AllocationTests)));
        Under(1_000, "a repeated member lookup", () => _documentation.For(typeof(TypeGeneration.TypeWalker).GetProperty("Count")!));
    }

    /// <summary>A type this walk has already seen is a dictionary hit, not a second walk of its members.</summary>
    [Fact]
    public void A_declared_type_costs_nothing_to_name_again()
    {
        var types = SyntheticTypes.Graph(20);
        var walker = new TypeGeneration.TypeWalker().Add(types[0]);

        Under(200, "re-adding a walked graph", () => walker.Add(types[0]));
    }

    private static void Under(long ceiling, string what, Action work)
    {
        var actual = Allocation.PerCall(work);

        Assert.True(actual <= ceiling, $"{what} allocates {actual:N0} bytes per call, over the {ceiling:N0} ceiling");
    }
}
