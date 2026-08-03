using SimpleTypeScript.Syntax;

namespace SimpleTypeScript.Types;

/// <summary>
/// A single string, as a type. Escaped through <see cref="TsSyntax"/> like any other string literal — a
/// literal type is one, and a wire value carrying a line terminator would otherwise end the declaration.
/// </summary>
internal sealed class TsStringLiteralType(string value) : TsType
{
    public override string Render() => TsSyntax.String(value);
}
