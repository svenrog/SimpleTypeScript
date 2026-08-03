using System.Text;

namespace SimpleTypeScript;

/// <summary>
/// An array literal. Kept on one line while every item fits on one — a list of locales reads better that way
/// than as a column of one entry each — and broken across lines as soon as anything inside is not.
/// </summary>
internal sealed class TsArrayValue(IReadOnlyList<TsValue> items) : TsValue
{
    internal override bool IsInline => items.All(item => item.IsInline);

    internal override void Write(StringBuilder builder, int depth)
    {
        if (items.Count == 0)
        {
            builder.Append("[]");
            return;
        }

        if (IsInline)
        {
            builder.Append('[');
            for (var index = 0; index < items.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                items[index].Write(builder, depth);
            }

            builder.Append(']');
            return;
        }

        builder.Append("[\n");
        foreach (var item in items)
        {
            Indent(builder, depth + 1);
            item.Write(builder, depth + 1);
            builder.Append(",\n");
        }

        Indent(builder, depth);
        builder.Append(']');
    }
}
