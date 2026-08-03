using SimpleTypeScript.TypeGeneration;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace SimpleTypeScript.Tests;

/// <summary>
/// The walk from C# types to declarations. What it must not do is describe a shape the producer does not
/// send: the generated file compiles either way, and the difference only appears at a runtime nobody is
/// watching.
/// </summary>
public sealed class TypeWalkerTests
{
    private static string Render(TypeWalkerOptions? options = null, params Type[] roots)
    {
        var module = new TsModule();
        new TypeWalker(options).Add(roots).Declare(module);

        return module.Render(TsComment.Empty());
    }

    [Fact]
    public void Writes_an_interface_per_shape_and_follows_what_its_members_reach()
    {
        var rendered = Render(null, typeof(Order));

        Assert.Contains("export interface Order {", rendered, StringComparison.Ordinal);
        Assert.Contains("export interface Line {", rendered, StringComparison.Ordinal);
        Assert.Contains("readonly lines: Line[];", rendered, StringComparison.Ordinal);
    }

    /// <summary>
    /// Names are the serializer's, not the compiler's. A generator reading the C# alone spells every member
    /// wrong the moment a naming policy is configured, and nothing says so until a field is undefined.
    /// </summary>
    [Fact]
    public void Reads_the_shape_that_is_serialized_rather_than_the_one_declared()
    {
        var rendered = Render(null, typeof(Order));

        Assert.Contains("readonly orderNumber: string;", rendered, StringComparison.Ordinal);
        Assert.Contains("readonly ref: string;", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("internalNote", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Writes_the_C_sharp_name_where_no_policy_is_given()
    {
        var rendered = Render(new TypeWalkerOptions { MemberNamingPolicy = null }, typeof(Line));

        Assert.Contains("readonly Quantity: number;", rendered, StringComparison.Ordinal);
    }

    /// <summary>
    /// A member that can arrive absent says so as <c>| null</c>, both when C# declares it nullable and when
    /// it is a nullable value type — the two are different in reflection and the same on the wire.
    /// </summary>
    [Fact]
    public void Says_where_a_value_may_be_absent()
    {
        var rendered = Render(null, typeof(Order));

        Assert.Contains("readonly note: string | null;", rendered, StringComparison.Ordinal);
        Assert.Contains("readonly shippedAt: string | null;", rendered, StringComparison.Ordinal);
        Assert.Contains("readonly total: number;", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Writes_an_enum_as_the_strings_the_wire_carries()
    {
        Assert.Contains(
            "export type Status = \"Open\" | \"Shipped\";",
            Render(null, typeof(Order)),
            StringComparison.Ordinal);

        Assert.Contains(
            "export type Status = \"open\" | \"shipped\";",
            Render(new TypeWalkerOptions { EnumNamingPolicy = JsonNamingPolicy.CamelCase }, typeof(Order)),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// The underlying values, for a producer that serializes an enum as a number. The member names cannot
    /// survive, because nothing on the wire carries them — and an alias is a second name for one value, so a
    /// union repeating it would say the same thing twice.
    /// </summary>
    [Fact]
    public void Writes_an_enum_as_its_numbers_where_that_is_what_the_wire_carries()
    {
        var rendered = Render(new TypeWalkerOptions { EnumStyle = EnumStyle.NumberUnion }, typeof(Order));

        Assert.Contains("export type Status = 0 | 1;", rendered, StringComparison.Ordinal);
        Assert.Contains("export type Priority = 0 | 5;", rendered, StringComparison.Ordinal);
    }

    /// <summary>
    /// A union does not exist at run time, so a consumer that has to iterate the set, index it or render a
    /// dropdown from it gets an object beside the type — and the type is read off the object rather than
    /// restated, so the two cannot drift.
    /// </summary>
    [Fact]
    public void Writes_an_enum_as_a_const_object_and_the_type_read_off_it()
    {
        var rendered = Render(new TypeWalkerOptions { EnumStyle = EnumStyle.ConstObject }, typeof(Order));

        Assert.Contains(
            "export const Status = {\n  \"Open\": \"Open\",\n  \"Shipped\": \"Shipped\",\n} as const;\n\n"
            + "export type Status = typeof Status[keyof typeof Status];",
            rendered,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// The key is what a consumer writes, the value is what the wire carries, and a naming policy moves only
    /// the second — so <c>Status.Open</c> keeps reading as the C# does while comparing equal to what arrives.
    /// </summary>
    [Fact]
    public void Keeps_the_member_name_a_consumer_spells_while_the_value_follows_the_policy()
    {
        var rendered = Render(
            new TypeWalkerOptions { EnumStyle = EnumStyle.ConstObject, EnumNamingPolicy = JsonNamingPolicy.CamelCase },
            typeof(Order));

        Assert.Contains("\"Open\": \"open\",", rendered, StringComparison.Ordinal);
        Assert.Contains("\"Shipped\": \"shipped\",", rendered, StringComparison.Ordinal);
    }

    /// <summary>
    /// A mapping is a leaf, which is what makes it the fix for a type carried as something other than its
    /// shape — an enum a producer writes in a form nothing here models — rather than a workaround for one.
    /// </summary>
    [Fact]
    public void Stops_at_a_mapped_type_and_declares_nothing_behind_it()
    {
        var rendered = Render(
            new TypeWalkerOptions { Mappings = new Dictionary<Type, TsType> { [typeof(Status)] = TsType.Number } },
            typeof(Order));

        Assert.Contains("readonly status: number;", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("export type Status", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Writes_a_dictionary_as_a_record_and_reaches_its_values()
    {
        var rendered = Render(null, typeof(Basket));

        Assert.Contains("readonly byRegion: Record<string, Line>;", rendered, StringComparison.Ordinal);
        Assert.Contains("export interface Line {", rendered, StringComparison.Ordinal);
    }

    /// <summary>A shape that reaches itself has to terminate, which is why a reference is recorded first.</summary>
    [Fact]
    public void Terminates_on_a_shape_that_reaches_itself()
    {
        var rendered = Render(null, typeof(Node));

        Assert.Contains("readonly parent: Node | null;", rendered, StringComparison.Ordinal);
        Assert.Contains("readonly children: Node[];", rendered, StringComparison.Ordinal);
    }

    /// <summary>
    /// A type the walk cannot describe is refused rather than written as <c>any</c>: a generated shape that
    /// checks nothing is worse than a build that stops, because only one of the two is noticed.
    /// </summary>
    [Fact]
    public void Refuses_a_type_it_has_no_shape_for()
    {
        var refusal = Assert.Throws<GenerationException>(() => Render(null, typeof(Untyped)));

        Assert.Contains(nameof(TypeWalkerOptions.Mappings), refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Declares_in_name_order_whatever_order_the_walk_reached_them_in()
    {
        var rendered = Render(null, typeof(Order));

        Assert.True(
            rendered.IndexOf("interface Line", StringComparison.Ordinal)
            < rendered.IndexOf("interface Order", StringComparison.Ordinal));
    }

    [Fact]
    public void Leaves_out_the_readonly_a_consumer_that_builds_the_payload_does_not_want()
    {
        var rendered = Render(new TypeWalkerOptions { ReadOnlyMembers = false }, typeof(Line));

        Assert.Contains("  quantity: number;", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("readonly", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Names_a_declaration_the_way_the_caller_asks()
    {
        var rendered = Render(new TypeWalkerOptions { Name = type => $"Api{type.Name}" }, typeof(Line));

        Assert.Contains("export interface ApiLine {", rendered, StringComparison.Ordinal);
    }

    private enum Status
    {
        Open,
        Shipped,
    }

    /// <summary>Values of its own, and an alias sharing one.</summary>
    private enum Priority
    {
        Normal = 0,
        Standard = 0,
        Urgent = 5,
    }

    private sealed class Order
    {
        public string OrderNumber { get; init; } = string.Empty;

        [JsonPropertyName("ref")]
        public string Reference { get; init; } = string.Empty;

        [JsonIgnore]
        public string InternalNote { get; init; } = string.Empty;

        public string? Note { get; init; }

        public DateTimeOffset? ShippedAt { get; init; }

        public decimal Total { get; init; }

        public Status Status { get; init; }

        public Priority Priority { get; init; }

        public IReadOnlyList<Line> Lines { get; init; } = [];
    }

    private sealed class Line
    {
        public int Quantity { get; init; }
    }

    private sealed class Basket
    {
        public IReadOnlyDictionary<string, Line> ByRegion { get; init; } = new Dictionary<string, Line>();
    }

    private sealed class Node
    {
        public Node? Parent { get; init; }

        public IReadOnlyList<Node> Children { get; init; } = [];
    }

    private sealed class Untyped
    {
        public object Payload { get; init; } = new();
    }
}
