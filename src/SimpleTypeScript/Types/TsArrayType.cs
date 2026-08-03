namespace SimpleTypeScript.Types;

/// <summary>
/// An array of another type. The postfix <c>[]</c> binds tighter than anything an element can be, so an
/// element that does not close with its own syntax is parenthesised and the rest are left alone.
/// </summary>
internal sealed class TsArrayType(TsType item) : TsType
{
    public override string Render() =>
        item.RequiresParentheses ? $"({item.Render()})[]" : $"{item.Render()}[]";
}
