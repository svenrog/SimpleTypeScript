using SimpleTypeScript.Syntax;
using System.Text;

namespace SimpleTypeScript;

/// <summary>
/// A comment, as a value that knows it is one. Text handed to a module as a bare string would be a comment
/// only by the convention of whatever prefixed it, and the compiler would have nothing to say when that
/// convention was wrong.
/// <para>
/// <b>The prefixing is where the safety is.</b> A line comment runs to the next line terminator, so text
/// carrying one ends the comment and everything after it becomes code — silently, in a file nobody reads
/// before running. ECMAScript ends a line on U+2028 and U+2029 as well as CR and LF, so a string that looks
/// like one line to C# can be four to the parser. Every terminator is therefore split on and re-prefixed,
/// and a block comment's terminator is neutralised the same way.
/// </para>
/// </summary>
public sealed class TsComment
{
    /// <summary>Everything ECMAScript ends a line on. CRLF is handled by dropping empties after the split.</summary>
    private static readonly string[] _terminators =
        ["\r\n", "\n", "\r", TsSyntax.LineSeparator.ToString(), TsSyntax.ParagraphSeparator.ToString()];

    private readonly IReadOnlyList<string> _lines;
    private readonly bool _isDoc;

    private TsComment(IReadOnlyList<string> lines, bool isDoc)
    {
        _lines = lines;
        _isDoc = isDoc;
    }

    /// <summary>Ordinary <c>//</c> comment lines — a file header, a note above a declaration.</summary>
    public static TsComment Lines(IEnumerable<string> text) =>
        new([.. text.SelectMany(line => Split(line, closeable: false))], isDoc: false);

    /// <summary>A <c>/** … */</c> doc comment, which editors surface at the use site.</summary>
    public static TsComment Doc(string text) => new([.. Split(text, closeable: true)], isDoc: true);

    /// <summary>Whether there is anything to write.</summary>
    internal bool IsEmpty => _lines.Count == 0;

    /// <summary>
    /// Appends the comment at <paramref name="depth"/>, each line ending in <c>\n</c>. The first line is
    /// indented like the rest: a comment always begins a line of its own, so unlike a value it never follows
    /// something already written.
    /// </summary>
    internal void Write(StringBuilder builder, int depth = 0)
    {
        if (IsEmpty)
        {
            return;
        }

        if (!_isDoc)
        {
            foreach (var line in _lines)
            {
                TsSyntax.AppendIndent(builder, depth);
                builder.Append("// ").Append(line).Append('\n');
            }

            return;
        }

        // One line is the common case and reads better closed on itself than opened over three.
        if (_lines.Count == 1)
        {
            TsSyntax.AppendIndent(builder, depth);
            builder.Append("/** ").Append(_lines[0]).Append(" */\n");
            return;
        }

        TsSyntax.AppendIndent(builder, depth);
        builder.Append("/**\n");
        foreach (var line in _lines)
        {
            TsSyntax.AppendIndent(builder, depth);
            builder.Append(" * ").Append(line).Append('\n');
        }

        TsSyntax.AppendIndent(builder, depth);
        builder.Append(" */\n");
    }

    /// <summary>
    /// One string as the lines a parser would see. Blank lines are kept — a header may space itself — but a
    /// wholly empty text contributes nothing rather than a bare <c>//</c>.
    /// <para>
    /// <paramref name="closeable"/> says whether the form being written can be ended from inside by
    /// <c>*/</c>. Only a block comment can, so only there is the sequence broken: doing it to a <c>//</c>
    /// line would rewrite text that was never dangerous, and a header quoting a glob or a regex is exactly
    /// where that shows up.
    /// </para>
    /// </summary>
    private static IEnumerable<string> Split(string text, bool closeable)
    {
        if (text.Length == 0)
        {
            return [];
        }

        var lines = text.Split(_terminators, StringSplitOptions.None);

        return closeable
            ? lines.Select(line => line.Replace("*/", "*\\/", StringComparison.Ordinal))
            : lines;
    }
}
