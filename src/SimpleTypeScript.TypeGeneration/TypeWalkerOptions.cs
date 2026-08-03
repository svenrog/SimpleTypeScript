using SimpleTypeScript.TypeGeneration.Documentation;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleTypeScript.TypeGeneration;

/// <summary>
/// How a walk reads the types it is given. Every default is what <c>System.Text.Json</c> does with no
/// configuration at all, so a generator that has not thought about naming gets what its API already sends.
/// </summary>
public sealed class TypeWalkerOptions
{
    /// <summary>
    /// How a property name reaches the wire. Camel case by default, matching the web defaults a JSON API is
    /// usually configured with; <c>null</c> writes the C# name as declared.
    /// </summary>
    public JsonNamingPolicy? MemberNamingPolicy { get; init; } = JsonNamingPolicy.CamelCase;

    /// <summary>
    /// How an enum member reaches the wire, where <see cref="EnumStyle"/> writes its name. <c>null</c> — the
    /// default — is the member name as written, which is what <c>JsonStringEnumConverter</c> does unless it
    /// is handed a policy of its own.
    /// </summary>
    public JsonNamingPolicy? EnumNamingPolicy { get; init; }

    /// <summary>
    /// What an enum is written as. The default is the union of the strings the wire carries; a producer that
    /// serializes numbers wants <see cref="TypeGeneration.EnumStyle.NumberUnion"/>, and a consumer that wants
    /// the set at run time as well as at compile time wants
    /// <see cref="TypeGeneration.EnumStyle.ConstObject"/>.
    /// <para>
    /// An enum that should not be generated at all is a mapping like any other type: name it in
    /// <see cref="Mappings"/>, and the walk stops there.
    /// </para>
    /// </summary>
    public EnumStyle EnumStyle { get; init; } = EnumStyle.StringUnion;

    /// <summary>
    /// Whether members are written <c>readonly</c>, for a shape describing what a consumer only ever
    /// receives. Off by default, which is what a generated type looks like everywhere else.
    /// <para>
    /// <b>It is as shallow as the language's own.</b> <c>readonly lines: Line[]</c> refuses
    /// <c>order.lines = []</c> and permits <c>order.lines.push(x)</c> — the assignment nobody writes and the
    /// mutation they do. Saying it properly would need the elements to be readonly as well, which nothing
    /// here can spell yet, so this is worth asking for only where the shallow half is what you wanted.
    /// </para>
    /// </summary>
    public bool ReadOnlyMembers { get; init; }

    /// <summary>
    /// When the producer leaves a member out of the payload altogether, mirroring the option of the same
    /// name on <c>JsonSerializerOptions</c>. <see cref="JsonIgnoreCondition.Never"/> by default, which is
    /// the serializer's own: every member is written, and one that is null is written as <c>null</c>.
    /// <para>
    /// This is the <em>presence</em> of a key, which TypeScript says with <c>?</c>, and it is a separate
    /// question from what may be in it. A producer configured to omit nulls sends no key at all, so its
    /// members are optional and never <c>null</c> where they do appear; left at the default the key is
    /// always there, and <c>T | null</c> is what can arrive in it.
    /// </para>
    /// <para>
    /// A member carrying <c>[JsonIgnore]</c> answers for itself, and one the API requires — <c>required</c>,
    /// <c>[JsonRequired]</c> or <c>[Required]</c> — is never optional whatever this says.
    /// </para>
    /// </summary>
    public JsonIgnoreCondition DefaultIgnoreCondition { get; init; } = JsonIgnoreCondition.Never;

    /// <summary>
    /// Types the walk stops at, merged over <see cref="TypeMappings.Default"/> — a type named here wins, so
    /// a mapping is also how a shape that is carried as something else says so.
    /// </summary>
    public IReadOnlyDictionary<Type, TsType> Mappings { get; init; } = new Dictionary<Type, TsType>();

    /// <summary>Where doc comments come from. None by default.</summary>
    public IDocumentationSource Documentation { get; init; } = NoDocumentation.Instance;

    /// <summary>
    /// What a declaration is called. The C# type name by default, which is what a consumer reading both
    /// sides expects to see; a generator with a prefix or a suffix convention says so here.
    /// </summary>
    public Func<Type, string> Name { get; init; } = type => type.Name;
}
