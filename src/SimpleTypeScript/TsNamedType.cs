namespace SimpleTypeScript;

/// <summary>A type written as its name: a primitive, or a reference the module imports.</summary>
internal sealed class TsNamedType(string name) : TsType
{
    public override string Render() => name;
}
