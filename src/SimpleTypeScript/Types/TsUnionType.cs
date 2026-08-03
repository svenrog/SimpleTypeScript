using System.Text;

namespace SimpleTypeScript.Types;

/// <summary>
/// Alternatives, one of which a value is. The first type here that does <em>not</em> close with its own
/// syntax: <c>"a" | "b"</c> inside an array has to be parenthesised, since <c>"a" | "b"[]</c> is a union with
/// an array in it — a different type that compiles just as well.
/// </summary>
internal sealed class TsUnionType(IReadOnlyList<TsType> members) : TsType
{
    internal override bool RequiresParentheses => members.Count > 1;

    internal override void Write(StringBuilder builder)
    {
        for (var index = 0; index < members.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(" | ");
            }

            members[index].Write(builder);
        }
    }
}
