namespace SimpleTypeScript.TypeGeneration;

/// <summary>
/// A declaration a generator read did not hold to what its output needs of it — a set that turned out empty,
/// two modules claiming one path, a shape with nothing in it.
/// <para>
/// Distinct from <see cref="TypeScriptException"/>, which says the emitter was asked for something the
/// language cannot spell. This one is about the <em>source</em>: the C# was readable and wrong. Everything
/// the pipeline can refuse arrives as this, so a host has one thing to catch and one sentence to print.
/// </para>
/// </summary>
public sealed class GenerationException : Exception
{
    /// <summary>Creates the failure with the sentence an operator sees.</summary>
    public GenerationException(string message) : base(message)
    {
    }

    /// <summary>Creates the failure from the one underneath it, keeping its sentence.</summary>
    public GenerationException(string message, Exception inner) : base(message, inner)
    {
    }
}
