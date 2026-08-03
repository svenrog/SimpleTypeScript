using SimpleTypeScript.Syntax;
using System.Text;

namespace SimpleTypeScript.Values;

/// <summary>A string literal, escaped for ECMAScript source by <see cref="TsSyntax"/>.</summary>
internal sealed class TsStringValue(string value) : TsValue
{
    internal override bool IsInline => true;

    internal override void Write(StringBuilder builder, int depth) => TsSyntax.AppendString(builder, value);
}
