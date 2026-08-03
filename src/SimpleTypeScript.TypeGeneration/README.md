# SimpleTypeScript.TypeGeneration 📘

[![Platform](https://img.shields.io/badge/Platform-.NET%2010-blue.svg?style=flat)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/SimpleTypeScript.TypeGeneration)](https://www.nuget.org/packages/SimpleTypeScript.TypeGeneration)
[![License: MIT](https://img.shields.io/github/license/svenrog/SimpleTypeScript)](https://github.com/svenrog/SimpleTypeScript/blob/master/LICENSE.txt)

Generate TypeScript from C# types: give it roots, and it follows what their members reach — an interface per
shape, a string union per enum — written through
[`SimpleTypeScript`](https://www.nuget.org/packages/SimpleTypeScript).

```csharp
var module = new TsModule();

new TypeWalker(new TypeWalkerOptions
    {
        Documentation = new XmlDocumentationSource(),
        Mappings = new Dictionary<Type, TsType> { [typeof(Money)] = TsType.String },
    })
    .Add(typeof(Order), typeof(Customer))
    .Declare(module);

File.WriteAllText(path, module.Render(TsComment.Lines(["GENERATED — do not edit."])));
```

Given this C#:

```csharp
/// <summary>One order, as the API returns it.</summary>
public sealed class Order
{
    /// <summary>Assigned when the order is placed.</summary>
    public Guid Id { get; init; }

    [JsonPropertyName("ref")]
    public string Reference { get; init; } = string.Empty;

    [JsonIgnore]
    public string InternalNote { get; init; } = string.Empty;

    public DateTimeOffset? ShippedAt { get; init; }

    public OrderStatus Status { get; init; }

    public IReadOnlyList<Line> Lines { get; init; } = [];
}
```

it writes this:

```ts
/** One order, as the API returns it. */
export interface Order {
  /** Assigned when the order is placed. */
  readonly id: string;
  readonly ref: string;
  readonly shippedAt: string | null;
  readonly status: OrderStatus;
  readonly lines: Line[];
}

export type OrderStatus = "Open" | "Shipped";
```

**It reads the shape that is serialized, not only the shape that is declared.** `[JsonIgnore]` drops a
member, `[JsonPropertyName]` renames it, and everything else takes the naming policy — camel case by default,
matching what a JSON API is usually configured with. A generator that reads the C# alone spells every member
wrong the moment a policy is set, and nothing says so until a field is `undefined`.

## What it decides, and how to overrule it

| | | `TypeWalkerOptions` |
| --- | --- | --- |
| A member name | camel case | `MemberNamingPolicy`, or `null` for the C# name |
| A nullable member | `T \| null`, for a nullable reference and a `Nullable<T>` alike | — |
| An enum | a union of the member names, which is what `JsonStringEnumConverter` writes | `EnumStyle` (below) and `EnumNamingPolicy` where a converter renames the members |
| A sequence | `T[]`; a dictionary is `Record<string, V>`, since a JSON key is a string whatever the C# key is | — |
| A BCL type | `TypeMappings.Default`: dates, `Guid`, `Uri`, every numeric, `byte[]` as its base64 string, `JsonElement` as `unknown` | `Mappings`, merged over the defaults |
| Anything else | refused rather than written as `any` | `Mappings` |
| A member's mutability | `readonly`, since a consumer usually receives these | `ReadOnlyMembers` |
| A declaration's name | the C# type name | `Name` |
| Doc comments | none | `Documentation`; `XmlDocumentationSource` reads the compiler's XmlDoc and flattens its markup to a sentence |

A mapped type is a **leaf**: nothing behind it is reached, which makes mapping the fix for a framework type
dragging its own graph into the output, and the way to say a type is carried as something other than its
shape. Declarations are emitted in name order and members in declaration order, so the file is byte-stable
across runs and machines.

## Enums

`EnumStyle` picks between three. The first two are decided by what the producer serializes; the third is a
preference about what a consumer wants beside the type.

```csharp
var options = new TypeWalkerOptions { EnumStyle = EnumStyle.ConstObject };
```

```ts
// StringUnion (default) — JsonStringEnumConverter
export type Status = "Open" | "Shipped";

// NumberUnion — an enum serialized by value; the member names cannot survive
export type Status = 0 | 1;

// ConstObject — a value to iterate, and the type read off it
export const Status = {
  "Open": "Open",
  "Shipped": "Shipped",
} as const;

export type Status = typeof Status[keyof typeof Status];
```

The const object's **key is what a consumer writes and its value is what the wire carries**, so
`EnumNamingPolicy` moves only the second: `Status.Open === "open"` still reads as the C# does. `Object.values(Status)`
is what a union alone cannot give you — a union has no run-time existence, so nothing can enumerate it.

**`export enum` is deliberately absent.** It is the one form with runtime semantics of its own, which makes it
the form a type-stripping loader — `erasableSyntaxOnly`, Node's own — refuses to run. An enum that should not
be generated at all is a mapping like any other type.

## Generators with more than one file

A build-time tool usually writes several — the types, a vocabulary, a palette. A module says what it is, and
the pipeline owns the banner, the directories and the writing:

```csharp
internal sealed class ApiTypesModule : TypeModule
{
    public override string FileName => "api/generated/index.ts";

    public override string Source => "the wire contracts";

    public override bool OwnsDirectory => true;

    protected override IEnumerable<Type> Roots => [typeof(Order), typeof(Customer)];

    protected override TypeWalkerOptions Options => new() { Documentation = new XmlDocumentationSource() };
}
```

```csharp
var writer = new ModuleWriter(outputDirectory);

foreach (var module in ModuleCatalog.From())
{
    var file = writer.Write(module);
    Console.WriteLine($"{file.Summary} -> {file.FileName}");
}
```

- **`ModuleCatalog`** discovers rather than listing — adding a generator is one class and no edit to a
  registry. Internal types count, and modules come back ordered by file name so a run reports the same way
  every time. Two modules claiming one path is a refusal, not a silent overwrite.
- **`ModuleWriter`** creates the directory a module names, and empties it first for a module that
  `OwnsDirectory` — a file the generator has stopped producing otherwise stays importable, and a stale shape
  is the one nobody notices.
- **`GeneratedHeader`** writes the do-not-edit banner. The default names the entry assembly, so a project
  renamed or moved takes its header with it; `GeneratedHeader.None` writes none.
- **Everything the pipeline can refuse is a `GenerationException`**, including what the emitter refuses
  underneath it — one thing for a host to catch and one sentence to print. Which half refused stays on
  `InnerException`.

A module that builds its declarations by hand implements `IGeneratedModule` directly; `TypeModule` is for the
common case where a module *is* a set of roots.

## Installing

```
dotnet add package SimpleTypeScript.TypeGeneration
```

`net10.0`. This half reflects, which is why it is a package of its own: the emitter reflects over nothing,
and a consumer publishing NativeAOT keeps that by taking only the emitter.

## License

MIT. See [LICENSE.txt](LICENSE.txt).
