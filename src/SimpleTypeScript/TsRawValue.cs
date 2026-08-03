using System.Text;

namespace SimpleTypeScript;

/// <summary>
/// A value already in its emitted form — a keyword, or a number <see cref="TsSyntax"/> has formatted. Only
/// reachable through <see cref="TsValue"/>'s factories, so nothing outside this assembly can put arbitrary
/// text into a module by this door.
/// </summary>
internal sealed class TsRawValue(string text) : TsValue
{
    internal override bool IsInline => true;

    internal override void Write(StringBuilder builder, int depth) => builder.Append(text);
}
