# Changelog

All notable changes to this project are documented here.

## 0.5.0

- **`EnumStyle`**: a C# enum is written as a string union (the default, unchanged), as a union of its
  underlying numbers, or as a `const` object with the type read off it —
  `typeof Status[keyof typeof Status]`. The last is what a consumer needs to iterate or index the set, which a
  union cannot do because it does not exist at run time; its keys stay the C# member names while its values
  follow `EnumNamingPolicy`, so it reads like the C# and compares equal to the wire. `export enum` is
  deliberately not offered — it is the one form a type-stripping loader refuses to run.
- `TsType.NumberLiteral` and `TsType.ValuesOf` in the emitter, which is all the above needed.
- `TsComment.Empty()`, so a module opening with no banner says so rather than passing no lines.
- `TsModule.Render()` takes no header at all: omitting one writes the plainest do-not-edit notice, since
  anything the emitter builds is generated. A banner is followed by one blank line, and no banner by none.

## 0.4.0

The rest of what a generator was writing for itself, in `SimpleTypeScript.TypeGeneration.Modules`:

- `IGeneratedModule` — one generated file, stating only what is its own — with `TypeModule` for the common
  case where a module *is* a set of roots.
- `ModuleCatalog` discovers modules in an assembly rather than being handed a list, ordered by file name, and
  refuses two that claim one path or an assembly that declares none.
- `ModuleWriter` builds, banners, creates the directory a module names, and empties it first for a module
  that `OwnsDirectory`.
- `GeneratedHeader` with `AssemblyGeneratedHeader` (the default, naming the entry assembly) and `None`.
- `GenerationException` — a source declaration that did not hold, as distinct from a name TypeScript cannot
  spell. Everything the pipeline refuses arrives as one, so a host catches one thing.
- The documentation sources moved to `SimpleTypeScript.TypeGeneration.Documentation`.

## 0.3.0

- **`SimpleTypeScript.TypeGeneration`**, a second package: `TypeWalker` follows a set of C# roots and
  declares what their members reach — an interface per shape, a string union per enum. It reads what
  `System.Text.Json` serializes rather than only what the C# declares (`[JsonIgnore]`, `[JsonPropertyName]`,
  the naming policy), maps the BCL types a DTO is made of, and refuses a type it has no shape for rather than
  writing `any`. Doc comments are opt-in through `IDocumentationSource`; `XmlDocumentationSource` reads the
  compiler's XmlDoc and flattens its markup to a sentence.
- Separate from the emitter because it reflects, and the emitter's claim is that it does not.

## 0.2.0

Shapes, not only values — enough to generate DTOs, which is what a consumer was running a second generator
for:

- `TsModule.Interface` and `TsModule.TypeAlias`, with `TsMember` for a property signature: `readonly`, a doc
  comment of its own, and a name written bare where it is an identifier and quoted where it is not.
- `TsType.StringLiteral` and `TsType.Union` — what a closed set becomes once a JSON wire carries it as a
  string. A union is the first type here that does not close with its own syntax, so `TsType` now tracks
  whether a container has to parenthesise: `("a" | "b")[]`, and nothing else.
- Refusals for the shapes that would check nothing: an interface with no members, one that declares a member
  twice, a union with no alternatives.

## 0.1.0

First release. The emitter behind a generated-module pipeline that was writing TypeScript out of C#
declarations, extracted once it had proved it knows nothing of the product it was written in:

- `TsModule`, `TsValue`, `TsType` and `TsComment` — a module held as declarations rather than as text, so
  what it *is* stays inspectable until it is written.
- String literals escaped for ECMAScript source rather than JSON, which is where U+2028, U+2029 and a lone
  surrogate stop being the serializer's problem.
- Comments that cannot be closed from inside, on every line terminator ECMAScript recognises.
- Numbers written invariant and round-trippable, with the three values that have no literal form refused.
- `\n` endings and caller-decided ordering, so the same declarations produce the same bytes on any machine.
