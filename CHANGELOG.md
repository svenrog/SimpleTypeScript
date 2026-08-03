# Changelog

All notable changes to this project are documented here.

## 1.0.0

**The API is settled.** From here, removing or changing a member in a way a consumer would notice is a major
version — and `EnablePackageValidation` holds the packages to that rather than leaving it a promise, once
this tag gives it a baseline to compare against. What that made worth finishing first is everything below.

**Breaking against 0.5.0**, and deliberately, since this is where that stops being free. Two signatures were
accreting parameters — a new option meant another argument and another break, which is the shape a settled
API cannot have:

- **`TsMember`** takes the name and type it *is*; how it is written is set on it.
  `new TsMember("id", TsType.String) { IsReadOnly = true, Doc = "The identity." }`. Two trailing booleans
  read as a bare `true` at a call site that said which was which nowhere. `Name` and `Type` are public with
  it. The next modifier is a property rather than a signature.
- **`TsModule.Const`** loses `asConst`, and `TsValue.AsConst(value)` carries the assertion instead — which
  is where the language puts it, on the expression rather than the declaration. `type` and `asConst` were
  mutually exclusive and refused each other at run time; two parameters that cannot both be set is a
  signature describing the wrong thing. Nothing about the generated output changed.

Both packages now target **`net8.0` and `net10.0`**, and the test suite runs on each. A build-time generator
is consumed by whatever the team's application targets, and the newest framework is not usually it. The only
difference between them is that the two ignore conditions naming a direction — `WhenWriting` and
`WhenReading` — arrived in .NET 10 along with the serializer that honours them, so on `net8.0` there is no
producer configured that way for a shape to describe.

The emitter's AOT compatibility is published rather than asserted: `tests/SimpleTypeScript.AotConsumer` is
compiled native on every build. `EnablePackageValidation` is on, and gains a baseline once 1.0.0 is tagged.

Fixes for output that was wrong without saying so. Each of these could produce a module that compiled, so a
consumer checking against it saw no sign.

- **Two types written as one declaration** are refused. Their simple names are what reaches the output and
  the output has no namespaces, so `Orders.Summary` and `Invoices.Summary` left one interface with both
  referring to it — every member of one checked against the shape of the other. `TypeWalkerOptions.Name`
  tells them apart. `TsModule` refuses the same collision underneath, per declaration space: a `const` and a
  type of one name stays the legitimate pair a generated enum writes.
- **A module that owns the output directory itself** is refused rather than emptying it. `OwnsDirectory`
  deletes the module's directory before the write, and for a module naming a file directly under the root
  that directory was the consumer's own source tree. A file name resolving outside the root is refused too.
- **`[JsonIgnore]` is read as the condition it is.** Only the unconditional form drops a member;
  `WhenWritingNull` and `WhenWritingDefault` make it optional — `?`, since the producer omits it rather than
  sending it empty — and `Never` means the opposite of the attribute's name, so the member stays required.
  `[JsonExtensionData]` is no longer written as a member, since the serializer flattens it into the object
  holding it.
- **A nullable value type is `T | null` wherever it appears**, not only as a member. `IReadOnlyList<int?>`
  and `IReadOnlyDictionary<string, int?>` had no shape at all and failed the build.
- **What a collection holds is read for nullability too.** Whether null belongs is a property of the
  position rather than of the type — the same `string` is nullable inside one list and not inside the next —
  and only the member's own annotation was being read, so `string?[]` came out `string[]`. It now reads the
  whole annotation: `(string | null)[]`, `Record<string, string | null>`, and `string[]?` still
  `string[] | null`, which is the different thing it always was.
- **A doc comment on a type declared inside another is found.** Nesting is a `+` in `Type.FullName` and a
  `.` in the file the compiler writes, so every nested shape — which is where a DTO usually lives — was
  looked up under a name the file never carries and silently had no documentation.
- **`TypeWalkerOptions.DefaultIgnoreCondition`**, mirroring the option of the same name on
  `JsonSerializerOptions`, so a producer configured to omit nulls generates `note?: string` rather than
  `note: string | null`. Absence and null are separate questions and the answer follows the producer, not
  TypeScript convention: left at the serializer's own default every key is written and a null one is written
  as `null`, which is what the walk already said. A member the condition leaves out loses its `| null` with
  the key — an absent key never arrives holding one. `Always` is refused here exactly as the serializer
  refuses it.
- **`required`, `[JsonRequired]` and `[Required]` are never written optional.** Only the first two are the
  serializer's to enforce, but a member published as required is one a consumer is answered with an error
  for omitting. None of them touches the *value*: `[Required]` is validation, it runs on the way in and
  leaves what is written alone, so a member the C# declares nullable stays `T | null`.
- **`JsonNode`, `JsonObject`, `JsonArray` and `JsonValue`** join `JsonElement` as `unknown`. Any other type
  under `System.` or `Microsoft.` is now refused instead of walked: nothing there is a payload, so what came
  out described a framework implementation. Carrying one on the wire is a `Mappings` entry, which is where
  the caller says what it is carried as.
- `TsMember` takes `isOptional`, which is what the above needed.

Nothing else generates differently; what changed is what it costs.

- A string literal is escaped by copying the text between escapes rather than by reading it a character at a
  time, and a type writes itself into the module's buffer rather than returning the text of each level for
  the level above it to copy. A module of a thousand interfaces renders in 0.4x the time and 0.6x the
  allocation; a vocabulary module, 0.3x and 0.7x. The one case that costs more is a string that is mostly
  characters needing an escape, which no generated module has been.
- `XmlDocumentationSource` reads its file as a stream instead of holding the whole document. The file is
  every documented member of an assembly, and only summaries are ever asked for.
- The walk asks a type for its interfaces once rather than four times, and asks whether a member is ignored
  without constructing the attribute that says so.

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
