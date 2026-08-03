using System.Reflection;

namespace SimpleTypeScript.TypeGeneration;

/// <summary>
/// No doc comments at all, which is the default: an assembly is not obliged to ship its XmlDoc, and a walk
/// that assumed otherwise would be reading whatever file happened to sit beside it.
/// </summary>
public sealed class NoDocumentation : IDocumentationSource
{
    /// <summary>The one instance; it holds nothing.</summary>
    public static IDocumentationSource Instance { get; } = new NoDocumentation();

    private NoDocumentation()
    {
    }

    /// <inheritdoc />
    public string? For(Type type) => null;

    /// <inheritdoc />
    public string? For(PropertyInfo property) => null;
}
