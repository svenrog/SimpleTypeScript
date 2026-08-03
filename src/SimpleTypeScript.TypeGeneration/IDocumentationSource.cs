using System.Reflection;

namespace SimpleTypeScript.TypeGeneration;

/// <summary>
/// Where a declaration's doc comment comes from. A seam rather than a setting because the C# XmlDoc is only
/// the usual answer: a generator with its own catalog of descriptions, or one that wants none, says so here
/// instead of the walk deciding for it.
/// </summary>
public interface IDocumentationSource
{
    /// <summary>What to write above the declaration, or <c>null</c> for nothing.</summary>
    string? For(Type type);

    /// <summary>What to write above the member, or <c>null</c> for nothing.</summary>
    string? For(PropertyInfo property);
}
