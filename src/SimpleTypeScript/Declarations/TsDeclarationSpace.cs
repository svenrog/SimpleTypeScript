namespace SimpleTypeScript.Declarations;

/// <summary>
/// Which of TypeScript's two namespaces a declaration binds in. They are separate, which is why a
/// <c>const</c> and a type of one name is a legitimate pair rather than a collision — and why two types of
/// one name is not.
/// </summary>
internal enum TsDeclarationSpace
{
    /// <summary>Bound at run time: what a <c>const</c> declares.</summary>
    Value,

    /// <summary>Bound at compile time only: an <c>interface</c> or a <c>type</c> alias.</summary>
    Type,
}
