namespace SimpleTypeScript.Types;

/// <summary>
/// <c>typeof X[keyof typeof X]</c>: the union of what an object's properties hold, named from the object
/// rather than restated beside it. What a <c>const</c> object and a type of the same name are, together, when
/// a consumer wants both a value to iterate and a type to check against.
/// <para>
/// It closes with its own indexed access, so an array of one needs no parentheses: <c>A[B][]</c> is an array
/// of <c>A[B]</c>, not an index into an array.
/// </para>
/// </summary>
internal sealed class TsValuesOfType(string name) : TsType
{
    public override string Render() => $"typeof {name}[keyof typeof {name}]";
}
