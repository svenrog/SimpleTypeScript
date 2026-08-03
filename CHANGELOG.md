# Changelog

All notable changes to this project are documented here.

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
