using System.Text;

namespace SimpleTypeScript.Types;

/// <summary>
/// A generic type applied to its arguments. Its own angle brackets close it, so it never needs
/// parenthesising however loose the arguments inside are.
/// </summary>
internal sealed class TsGenericType(string name, IReadOnlyList<TsType> arguments) : TsType
{
    internal override void Write(StringBuilder builder)
    {
        builder.Append(name).Append('<');

        for (var index = 0; index < arguments.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            arguments[index].Write(builder);
        }

        builder.Append('>');
    }
}
