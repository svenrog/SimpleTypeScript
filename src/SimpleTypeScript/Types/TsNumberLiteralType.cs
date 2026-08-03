using SimpleTypeScript.Syntax;
using System.Text;

namespace SimpleTypeScript.Types;

/// <summary>
/// A single number, as a type. Spelled by <see cref="TsSyntax"/> like any other number literal, so it is
/// invariant and round-trippable — a literal type that reads differently to a parser than to the generator
/// is a type nothing can ever be assigned to.
/// </summary>
internal sealed class TsNumberLiteralType(double value) : TsType
{
    internal override void Write(StringBuilder builder) => builder.Append(TsSyntax.Number(value));
}
