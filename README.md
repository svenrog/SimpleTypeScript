# SimpleTypeScript

Generated TypeScript, spelled correctly once. Two packages: one writes the language, the other reads C#
types into it.

| Package | What it is |
| --- | --- |
| [**SimpleTypeScript**](src/SimpleTypeScript/README.md) | The emitter. Build a module out of declarations, types and values; it decides the escaping, the quoting, the numbers and the layout. No dependencies, and it reflects over nothing. |
| [**SimpleTypeScript.TypeGeneration**](src/SimpleTypeScript.TypeGeneration/README.md) | The generator. Walk a set of C# roots into interfaces and unions — reading the shape `System.Text.Json` serializes — plus the module pipeline a build-time tool would otherwise write for itself. |

```csharp
var module = new TsModule();

new TypeWalker(new TypeWalkerOptions { Documentation = new XmlDocumentationSource() })
    .Add(typeof(Order))
    .Declare(module);

File.WriteAllText(path, module.Render(TsComment.Lines(["GENERATED — do not edit."])));
```

```ts
// GENERATED — do not edit.

/** One order, as the API returns it. */
export interface Order {
  readonly id: string;
  readonly shippedAt: string | null;
  readonly status: OrderStatus;
}

export type OrderStatus = "Open" | "Shipped";
```

## Why

Generated code is compiled before anyone reads it, so a rule that is merely *probably* right produces a file
that is broken where nobody is looking. What each package owns is the set of those rules that is easy to get
wrong:

- A string literal is escaped for **ECMAScript source, not JSON** — the two differ on U+2028, U+2029 and a
  lone surrogate.
- A comment cannot be closed from inside it, on any terminator the language recognises.
- The shape on the wire is the **serializer's**, not the compiler's: `[JsonIgnore]`, `[JsonPropertyName]` and
  the naming policy all change it.
- Output is byte-stable — `\n` endings, name-ordered declarations — so regenerating on another machine does
  not rewrite a file that did not change.

Each package's README has the detail, and every C# example in all three is compiled by the test suite, so
none of them can drift from the API.

## Building

```
dotnet build SimpleTypeScript.slnx
dotnet test SimpleTypeScript.slnx
dotnet run --project tests/SimpleTypeScript.Benchmarks -c Release
```

`net10.0` throughout. Releases are tagged `v*`; each tag publishes both packages.

## License

MIT. See [LICENSE.txt](LICENSE.txt).
