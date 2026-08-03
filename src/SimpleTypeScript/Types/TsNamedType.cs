using System.Text;

namespace SimpleTypeScript.Types;

/// <summary>A type written as its name: a primitive, or a reference the module imports.</summary>
internal sealed class TsNamedType(string name) : TsType
{
    internal override void Write(StringBuilder builder) => builder.Append(name);
}
