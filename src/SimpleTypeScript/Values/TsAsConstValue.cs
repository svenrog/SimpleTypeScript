using System.Text;

namespace SimpleTypeScript.Values;

/// <summary>
/// A value asserted as its own literal type. It sits on the value rather than on the declaration because
/// that is where the language puts it — <c>as const</c> is an assertion on the expression, and a declaration
/// that carried it as a flag would be describing something one level up from what it is.
/// </summary>
internal sealed class TsAsConstValue(TsValue value) : TsValue
{
    internal override bool IsInline => value.IsInline;

    internal override void Write(StringBuilder builder, int depth)
    {
        value.Write(builder, depth);
        builder.Append(" as const");
    }
}
