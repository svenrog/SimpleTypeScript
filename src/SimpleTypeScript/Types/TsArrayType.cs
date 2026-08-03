using System.Text;

namespace SimpleTypeScript.Types;

/// <summary>
/// An array of another type. The postfix <c>[]</c> binds tighter than anything an element can be, so an
/// element that does not close with its own syntax is parenthesised and the rest are left alone.
/// </summary>
internal sealed class TsArrayType(TsType item) : TsType
{
    internal override void Write(StringBuilder builder)
    {
        if (item.RequiresParentheses)
        {
            builder.Append('(');
            item.Write(builder);
            builder.Append(')');
        }
        else
        {
            item.Write(builder);
        }

        builder.Append("[]");
    }
}
