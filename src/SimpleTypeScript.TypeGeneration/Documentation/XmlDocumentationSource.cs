using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace SimpleTypeScript.TypeGeneration.Documentation;

/// <summary>
/// The <c>&lt;summary&gt;</c> a type or member carries, read from the XmlDoc file the compiler wrote beside
/// its assembly. Worth carrying because the C# already documents the shape, and the alternative is
/// documenting the same member twice in two languages.
/// <para>
/// Flattened to a sentence rather than reproduced: doc XML is markup for a different renderer, and passing it
/// through puts raw <c>&lt;para&gt;</c> tags and fully qualified crefs into a TypeScript comment. A
/// <c>&lt;see cref&gt;</c> becomes the member's own name, which is what the sentence was reading as anyway.
/// </para>
/// </summary>
public sealed class XmlDocumentationSource : IDocumentationSource
{
    /// <summary>Punctuation that closes what precedes it, and so must not be left standing off from it.</summary>
    private static readonly char[] _closing = ['.', ',', ';', ':', ')', '!', '?'];

    private readonly Dictionary<string, Dictionary<string, string>> _byAssembly = [];
    private readonly string _directory;

    /// <summary>
    /// Reads <c>&lt;assembly&gt;.xml</c> from <paramref name="directory"/>, which defaults to where the
    /// running application was loaded from — the same place a project reference puts it.
    /// </summary>
    public XmlDocumentationSource(string? directory = null) =>
        _directory = directory ?? AppContext.BaseDirectory;

    /// <inheritdoc />
    public string? For(Type type) => Lookup(type.Assembly, $"T:{type.FullName}");

    /// <inheritdoc />
    public string? For(PropertyInfo property) =>
        Lookup(property.DeclaringType!.Assembly, $"P:{property.DeclaringType!.FullName}.{property.Name}");

    private string? Lookup(Assembly assembly, string key) =>
        Entries(assembly).TryGetValue(key, out var summary) ? summary : null;

    /// <summary>
    /// Every summary in an assembly's documentation file, read once. A missing file is an empty set rather
    /// than a failure: an assembly that ships no XmlDoc is a member without a comment, not a broken build.
    /// </summary>
    private Dictionary<string, string> Entries(Assembly assembly)
    {
        var name = assembly.GetName().Name!;
        if (_byAssembly.TryGetValue(name, out var cached))
        {
            return cached;
        }

        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        var path = Path.Combine(_directory, $"{name}.xml");
        if (File.Exists(path))
        {
            foreach (var member in XDocument.Load(path).Descendants("member"))
            {
                var key = member.Attribute("name")?.Value;
                var summary = member.Element("summary");
                if (key is null || summary is null)
                {
                    continue;
                }

                var text = Flatten(summary);
                if (text.Length > 0)
                {
                    entries[key] = text;
                }
            }
        }

        _byAssembly[name] = entries;

        return entries;
    }

    /// <summary>The element's text as one sentence, its own markup resolved and its whitespace normalized.</summary>
    private static string Flatten(XElement element)
    {
        var builder = new StringBuilder();
        Append(element, builder);

        var words = builder.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        var sentence = new StringBuilder();
        foreach (var word in words)
        {
            // Resolving an element leaves a space on both sides of it, which reads as a gap in front of the
            // comma that followed the tag rather than as the separator it is everywhere else.
            if (sentence.Length > 0 && !_closing.Contains(word[0]))
            {
                sentence.Append(' ');
            }

            sentence.Append(word);
        }

        return sentence.ToString();
    }

    private static void Append(XElement element, StringBuilder builder)
    {
        foreach (var node in element.Nodes())
        {
            switch (node)
            {
                case XText text:
                    builder.Append(text.Value);
                    break;

                case XElement child when child.Name == "see" || child.Name == "seealso":
                    // A cref is a fully qualified C# name, which means nothing on the other side of the wire.
                    // The last segment is the member the sentence was naming.
                    var reference = child.Attribute("cref")?.Value
                        ?? child.Attribute("langword")?.Value
                        ?? string.Empty;
                    builder.Append(' ').Append(reference.Split('.')[^1]).Append(' ');
                    break;

                case XElement child:
                    Append(child, builder);
                    builder.Append(' ');
                    break;
            }
        }
    }
}
