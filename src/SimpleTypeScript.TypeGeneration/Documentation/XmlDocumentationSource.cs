using System.Reflection;
using System.Text;
using System.Xml;

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
/// <para>
/// <b>One instance is one thread's.</b> Each assembly's file is read once and kept, and that cache is not
/// guarded — sharing one across concurrent walks is what a second instance costs nothing to avoid.
/// </para>
/// </summary>
public sealed class XmlDocumentationSource : IDocumentationSource
{
    /// <summary>Punctuation that closes what precedes it, and so must not be left standing off from it.</summary>
    private const string _closing = ".,;:)!?";

    private readonly Dictionary<string, Dictionary<string, string>> _byAssembly = [];
    private readonly string _directory;

    /// <summary>
    /// Reads <c>&lt;assembly&gt;.xml</c> from <paramref name="directory"/>, which defaults to where the
    /// running application was loaded from — the same place a project reference puts it.
    /// </summary>
    public XmlDocumentationSource(string? directory = null) =>
        _directory = directory ?? AppContext.BaseDirectory;

    /// <inheritdoc />
    public string? For(Type type) => Lookup(type.Assembly, $"T:{Named(type)}");

    /// <inheritdoc />
    public string? For(PropertyInfo property) =>
        Lookup(property.DeclaringType!.Assembly, $"P:{Named(property.DeclaringType!)}.{property.Name}");

    /// <summary>
    /// The type as a documentation key names it. Nesting is a <c>+</c> in a runtime name and a <c>.</c> in
    /// the file, so a nested shape asked for by the first is looked up under a name the file never carries.
    /// </summary>
    private static string Named(Type type) => type.FullName!.Replace('+', '.');

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

        var path = Path.Combine(_directory, $"{name}.xml");
        var entries = File.Exists(path) ? Read(path) : new Dictionary<string, string>(StringComparer.Ordinal);

        _byAssembly[name] = entries;

        return entries;
    }

    /// <summary>
    /// The summaries in one documentation file, read as a stream. Only the summary being flattened is held:
    /// the document is every member of an assembly, and none of the rest of it is ever asked about.
    /// </summary>
    private static Dictionary<string, string> Read(string path)
    {
        var entries = new Dictionary<string, string>(StringComparer.Ordinal);

        using var reader = XmlReader.Create(path);

        string? key = null;
        var depth = -1;

        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            if (reader.Name == "member")
            {
                key = reader.GetAttribute("name");
                depth = reader.Depth;
            }
            else if (key is not null && reader.Name == "summary" && reader.Depth == depth + 1)
            {
                var text = Summary(reader);
                if (text.Length > 0)
                {
                    entries[key] = text;
                }

                // The first summary is the member's; a second one is inside something this does not read.
                key = null;
            }
        }

        return entries;
    }

    /// <summary>
    /// The element the reader stands on, as one sentence, its own markup resolved and its whitespace
    /// normalized. Returns with the reader on the element's end tag.
    /// </summary>
    private static string Summary(XmlReader reader)
    {
        if (reader.IsEmptyElement)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        var depth = reader.Depth;

        // A skipped subtree leaves the reader on the node after it, which is the one the next turn of the
        // loop is meant to look at rather than read past.
        var positioned = false;

        while (positioned || reader.Read())
        {
            positioned = false;

            switch (reader.NodeType)
            {
                case XmlNodeType.EndElement when reader.Depth == depth:
                    return Sentence(builder);

                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    builder.Append(reader.Value);
                    break;

                case XmlNodeType.Element when reader.Name is "see" or "seealso":
                    // A cref is a fully qualified C# name, which means nothing on the other side of the wire.
                    // The last segment is the member the sentence was naming.
                    var reference = reader.GetAttribute("cref")
                        ?? reader.GetAttribute("langword")
                        ?? string.Empty;
                    builder.Append(' ').Append(reference[(reference.LastIndexOf('.') + 1)..]).Append(' ');

                    if (!reader.IsEmptyElement)
                    {
                        reader.Skip();
                        positioned = true;
                    }

                    break;

                case XmlNodeType.Element when reader.IsEmptyElement:
                case XmlNodeType.EndElement:
                    builder.Append(' ');
                    break;
            }
        }

        return Sentence(builder);
    }

    private static string Sentence(StringBuilder text)
    {
        var words = text.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

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
}
