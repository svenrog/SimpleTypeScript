namespace SimpleTypeScript.TypeGeneration;

/// <summary>
/// How a C# enum is written. What the wire carries decides between the first two; the third is a preference
/// about what a consumer wants beside the type.
/// <para>
/// <c>export enum</c> is deliberately absent. It is the one form with runtime semantics of its own, which
/// makes it the one form a type-stripping loader — <c>erasableSyntaxOnly</c>, Node's own — refuses to run;
/// <see cref="ConstObject"/> is what that leaves, and it does more.
/// </para>
/// </summary>
public enum EnumStyle
{
    /// <summary>
    /// <c>export type Status = "Open" | "Shipped";</c> — what <c>JsonStringEnumConverter</c> puts on the
    /// wire, and what a consumer compares a received value against.
    /// </summary>
    StringUnion,

    /// <summary>
    /// <c>export type Status = 0 | 1;</c> — the underlying values, for a producer that serializes an enum as
    /// a number. The member names do not survive, because nothing on the wire carries them.
    /// </summary>
    NumberUnion,

    /// <summary>
    /// A <c>const</c> object and a type derived from it, under one name:
    /// <code>
    /// export const Status = { Open: "Open", Shipped: "Shipped" } as const;
    /// export type Status = typeof Status[keyof typeof Status];
    /// </code>
    /// The union is the same one <see cref="StringUnion"/> writes, and the object beside it is what a
    /// consumer iterates, indexes or renders a dropdown from — none of which a bare union can do, since it
    /// does not exist at run time.
    /// </summary>
    ConstObject,
}
