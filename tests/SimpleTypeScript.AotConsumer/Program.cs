// The emitter's AOT claim, published rather than asserted. `IsAotCompatible` turns the trim and AOT
// analyzers on, which is what catches a reflecting call at build time; only a real publish says the result
// links and runs. Every kind of declaration is written here so none of it can be trimmed as unreached.
using SimpleTypeScript;

var module = new TsModule()
    .Const("LOCALES", TsValue.AsConst(TsValue.Array(["en-US", "sv-SE"])))
    .TypeAlias("Status", TsType.Union([TsType.StringLiteral("Open"), TsType.StringLiteral("Shipped")]))
    .Interface(
        "Order",
        [
            new TsMember("id", TsType.String) { IsReadOnly = true, Doc = "The identity." },
            new TsMember("note", TsType.String) { IsReadOnly = true, IsOptional = true },
            new TsMember("status", TsType.ValuesOf("Status")),
            new TsMember("totals", TsType.Record(TsType.String, TsType.Number)),
            new TsMember("tags", TsType.ArrayOf(TsType.Union([TsType.String, TsType.Null]))),
        ],
        doc: "One order, as the API returns it.");

Console.Write(module.Render(TsComment.Lines(["GENERATED — do not edit."])));
