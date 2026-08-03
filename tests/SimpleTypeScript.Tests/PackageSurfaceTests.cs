using SimpleTypeScript.TypeGeneration;
using Xunit;

namespace SimpleTypeScript.Tests;

/// <summary>
/// What a consumer takes on by referencing either package. Both facts here are invisible in a diff and
/// expensive to get back once something depends on them being otherwise.
/// </summary>
public sealed class PackageSurfaceTests
{
    /// <summary>
    /// An attribute of ours would have to be applied to the types being described, which puts a codegen
    /// package into the dependency graph of the contracts — usually the assembly a solution most wants to
    /// keep clean. Everything the walk reads is already in the shared framework:
    /// <c>System.Text.Json</c>'s, the compiler's own <c>required</c>, and DataAnnotations' <c>[Required]</c>.
    /// So a per-member switch is a walk-level option, and marking up the C# is the shape to refuse.
    /// </summary>
    [Theory]
    [InlineData(typeof(TsModule))]
    [InlineData(typeof(TypeWalker))]
    public void Declares_no_attribute_a_consumer_would_have_to_reference_it_for(Type inPackage)
    {
        var attributes = inPackage.Assembly
            .GetTypes()
            .Where(type => typeof(Attribute).IsAssignableFrom(type))
            .Select(type => type.FullName)
            .ToArray();

        Assert.True(
            attributes.Length == 0,
            $"{inPackage.Assembly.GetName().Name} declares {string.Join(", ", attributes)}; a contracts "
            + "assembly has to reference this package to apply one");
    }
}
