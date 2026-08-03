namespace SimpleTypeScript;

/// <summary>
/// An array of another type. Every type this model carries closes with its own syntax, so the postfix
/// <c>[]</c> always attaches to the whole element and none of them needs parenthesising.
/// </summary>
internal sealed class TsArrayType(TsType item) : TsType
{
    public override string Render() => $"{item.Render()}[]";
}
