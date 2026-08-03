using SimpleTypeScript.TypeGeneration;
using Xunit;

namespace SimpleTypeScript.Tests.Performance;

/// <summary>
/// What each half costs as its input grows. A generator that is quick on the ten declarations a test writes
/// and slow on the four hundred a real contract reaches is one nobody finds until a build drags, and by then
/// the cause is nothing anybody suspects.
/// <para>
/// A <em>ratio</em> between two sizes, because that is the question: quadruple the input and linear work
/// lands near four. Absolute figures move with a framework and a machine; the shape of the curve does not.
/// </para>
/// </summary>
public sealed class ScalingTests
{
    /// <summary>Comfortably above linear, comfortably below quadratic.</summary>
    private const double _linearEnough = 6.0;

    [Fact]
    public void Rendering_stays_linear_in_the_declarations_it_holds()
    {
        var small = Allocation.PerCall(() => Render(100), calls: 5);
        var large = Allocation.PerCall(() => Render(400), calls: 5);

        var ratio = (double)large / small;

        Assert.True(
            ratio < _linearEnough,
            $"rendering 400 declarations costs {ratio:0.0}x what 100 do; the writer is touching what it has already written");
    }

    /// <summary>
    /// The walk records a reference before it walks a type's members, and that is what keeps a graph from
    /// being re-walked through every member that points into it. Broken, this ratio is where it shows.
    /// </summary>
    [Fact]
    public void Walking_stays_linear_in_the_types_it_reaches()
    {
        var small = Allocation.PerCall(() => Walk(100), calls: 3);
        var large = Allocation.PerCall(() => Walk(400), calls: 3);

        var ratio = (double)large / small;

        Assert.True(
            ratio < _linearEnough,
            $"walking 400 types costs {ratio:0.0}x what 100 do; a type is being reached more than once");
    }

    /// <summary>
    /// A member's type is written once however many members name it. The walk is over a graph, not a tree,
    /// and the difference between the two is a generator that finishes and one that does not.
    /// </summary>
    [Fact]
    public void Reaching_one_type_from_many_members_declares_it_once()
    {
        var types = SyntheticTypes.Graph(50);
        var module = new TsModule();
        var walker = new TypeWalker().Add(types[0]);
        walker.Declare(module);

        var declared = module
            .Render(TsComment.Empty())
            .Split('\n')
            .Where(line => line.StartsWith("export interface ", StringComparison.Ordinal))
            .Select(line => line["export interface ".Length..].TrimEnd(' ', '{'))
            .ToList();

        // By name rather than by count: a missing one is a branch the walk stopped following, a repeated one
        // is a type it walked twice, and a count says only that something is wrong.
        Assert.Equal(
            types.Select(type => type.Name).OrderBy(name => name, StringComparer.Ordinal),
            declared.OrderBy(name => name, StringComparer.Ordinal));
        Assert.Equal(walker.Count, declared.Count);
    }

    private static void Render(int declarations)
    {
        var module = new TsModule();
        for (var index = 0; index < declarations; index++)
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

        module.Render();
    }

    private static void Walk(int types)
    {
        var module = new TsModule();
        new TypeWalker().Add(SyntheticTypes.Graph(types)[0]).Declare(module);
        module.Render(TsComment.Empty());
    }
}
