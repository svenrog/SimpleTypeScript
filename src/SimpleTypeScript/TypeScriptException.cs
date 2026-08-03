namespace SimpleTypeScript;

/// <summary>
/// A module was asked for something TypeScript has no way to spell — a binding name that is not one, a number
/// with no literal form, a type this model does not describe. Thrown rather than emitted as text, because
/// generated code is read by a compiler before it is read by a person: a module that renders anyway is broken
/// where nobody is looking, while a refusal stops whatever build asked for it.
/// </summary>
public sealed class TypeScriptException : Exception
{
    /// <summary>Creates the failure with the sentence the generator's caller sees.</summary>
    public TypeScriptException(string message) : base(message)
    {
    }
}
