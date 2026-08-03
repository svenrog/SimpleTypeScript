using SimpleTypeScript.TypeGeneration.Documentation;
using System.Text.Json;

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
    /// Whether members are written <c>readonly</c>. On by default: a generated shape usually describes what
    /// a consumer <em>receives</em>, and a payload it also builds is the smaller half of the work.
    /// </summary>
    public bool ReadOnlyMembers { get; init; } = true;

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
