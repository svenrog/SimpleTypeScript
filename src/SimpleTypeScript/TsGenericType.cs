namespace SimpleTypeScript;

/// <summary>
/// A generic type applied to its arguments. Its own angle brackets close it, so it never needs
/// parenthesising however loose the arguments inside are.
/// </summary>
internal sealed class TsGenericType(string name, IReadOnlyList<TsType> arguments) : TsType
{
    public override string Render() =>
        $"{name}<{string.Join(", ", arguments.Select(argument => argument.Render()))}>";
}
