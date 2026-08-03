namespace SimpleTypeScript.Tests.Elsewhere;

/// <summary>
/// A second shape whose simple name is already taken. In its own namespace because that is the whole point:
/// C# tells the two apart and the generated module has no namespaces to tell them apart with.
/// </summary>
internal sealed class Line
{
    public string Label { get; init; } = string.Empty;
}
