using SimpleTypeScript.Syntax;
using System.Text;

namespace SimpleTypeScript.Values;

/// <summary>
/// An object literal, always across lines: a generated map is opened to check one entry, and a single long
/// line is the shape that can be neither read nor diffed.
/// <para>
/// Keys are quoted whether or not they would pass bare. Uniform quoting is what a reader scanning a
/// generated map wants, and the alternative pays a correctness risk — the rules for a bare property name are
/// not the rules for a binding — to save two characters.
/// </para>
/// </summary>
internal sealed class TsObjectValue(IReadOnlyList<KeyValuePair<string, TsValue>> entries) : TsValue
{
    internal override bool IsInline => false;

    internal override void Write(StringBuilder builder, int depth)
    {
        if (entries.Count == 0)
        {
            builder.Append("{}");
            return;
        }

        builder.Append("{\n");
        foreach (var (key, value) in entries)
        {
            Indent(builder, depth + 1);
            TsSyntax.AppendString(builder, key);
            builder.Append(": ");
            value.Write(builder, depth + 1);
            builder.Append(",\n");
        }

        Indent(builder, depth);
        builder.Append('}');
    }
}
