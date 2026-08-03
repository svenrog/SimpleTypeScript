using System.Globalization;
using System.Text;

namespace SimpleTypeScript.Syntax;

/// <summary>
/// The lexical rules of the language being emitted: how a string literal is escaped, what may appear
/// unquoted as a property name, and what a valid binding name is.
/// <para>
/// Escaping is for <b>ECMAScript source</b>, not for JSON, and the two differ where it matters. JSON permits
/// U+2028 and U+2029 raw inside a string; ECMAScript treats them as line terminators outside of one and
/// tooling that re-emits or concatenates a module can carry them out of the literal, so they are escaped
/// here. A lone surrogate is escaped for the same reason — it cannot be encoded as well-formed UTF-8, so
/// left raw it makes the file unreadable rather than merely wrong.
/// </para>
/// <para>
/// Everything else printable is written as itself. A generated vocabulary is read by whoever checks a
/// translation, and a Swedish set escaped to <c>å</c> is technically correct and useless to them.
/// </para>
/// </summary>
internal static class TsSyntax
{
    /// <summary>
    /// The two separators ECMAScript treats as line terminators. Written numerically because the alternative
    /// is a raw line terminator sitting in this file — the very thing the escape exists to keep out of the
    /// generated one, and invisible to anyone reading the source.
    /// </summary>
    internal const char LineSeparator = (char)0x2028;

    /// <inheritdoc cref="LineSeparator" />
    internal const char ParagraphSeparator = (char)0x2029;

    /// <summary>Two spaces, which is what hand-written TypeScript is conventionally formatted at.</summary>
    private const string _indentation = "  ";

    /// <summary>Indents to <paramref name="depth"/>, for anything writing on a line of its own.</summary>
    internal static void AppendIndent(StringBuilder builder, int depth) =>
        builder.Insert(builder.Length, _indentation, depth);

    /// <summary><paramref name="value"/> as a double-quoted string literal.</summary>
    internal static string String(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        AppendString(builder, value);

        return builder.ToString();
    }

    /// <summary>Appends <paramref name="value"/> as a double-quoted string literal.</summary>
    internal static void AppendString(StringBuilder builder, string value)
    {
        builder.Append('"');

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            switch (character)
            {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                case '\b': builder.Append("\\b"); break;
                case '\f': builder.Append("\\f"); break;
                case '\n': builder.Append("\\n"); break;
                case '\r': builder.Append("\\r"); break;
                case '\t': builder.Append("\\t"); break;

                default:
                    if (char.IsControl(character)
                        || character is LineSeparator or ParagraphSeparator
                        || IsLoneSurrogate(value, index))
                    {
                        AppendEscape(builder, character);
                    }
                    else
                    {
                        builder.Append(character);
                    }

                    break;
            }
        }

        builder.Append('"');
    }

    /// <summary>
    /// Whether a property name may be written bare. Conservative on purpose: a name that is merely
    /// <em>probably</em> fine quoted costs nothing, while one wrongly left bare is a syntax error in
    /// generated code nobody reads before running.
    /// </summary>
    internal static bool IsIdentifier(string name)
    {
        if (name.Length == 0 || (!IsIdentifierStart(name[0])))
        {
            return false;
        }

        for (var index = 1; index < name.Length; index++)
        {
            if (!IsIdentifierPart(name[index]))
            {
                return false;
            }
        }

        return !_reserved.Contains(name);
    }

    /// <summary>
    /// A number as ECMAScript spells it: invariant, round-trippable, and never one of the three values that
    /// have no literal form. <c>NaN</c> and the infinities are legal expressions but not literals, and a
    /// generated module that silently emitted one would parse and mean something else.
    /// </summary>
    internal static string Number(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new TypeScriptException($"{value} has no TypeScript literal form");
        }

        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    private static void AppendEscape(StringBuilder builder, char character) =>
        builder.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));

    private static bool IsLoneSurrogate(string value, int index)
    {
        var character = value[index];
        if (char.IsHighSurrogate(character))
        {
            return index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]);
        }

        return char.IsLowSurrogate(character) && (index == 0 || !char.IsHighSurrogate(value[index - 1]));
    }

    private static bool IsIdentifierStart(char character) =>
        char.IsLetter(character) || character is '_' or '$';

    private static bool IsIdentifierPart(char character) =>
        char.IsLetterOrDigit(character) || character is '_' or '$';

    /// <summary>
    /// Words that cannot be a binding name. A property name may be a reserved word in modern ECMAScript, so
    /// this over-quotes there — which is the harmless direction.
    /// </summary>
    private static readonly HashSet<string> _reserved = new(StringComparer.Ordinal)
    {
        "break", "case", "catch", "class", "const", "continue", "debugger", "default", "delete", "do",
        "else", "enum", "export", "extends", "false", "finally", "for", "function", "if", "import", "in",
        "instanceof", "new", "null", "return", "super", "switch", "this", "throw", "true", "try", "typeof",
        "var", "void", "while", "with", "yield", "let", "static", "await", "implements", "interface",
        "package", "private", "protected", "public",
    };
}
