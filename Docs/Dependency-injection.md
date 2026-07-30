# Dependency injection (DI)

[← Project README](../../README.md)

The DI from KludgeBox is used. Practically every class calls `Di.Process(this)` as the first line in
`_Ready()` (or in the constructor for non-nodes), after which the annotated fields are filled in:

| Attribute | What it injects |
|---|---|
| `[Child]` | A child node by field name (or `[Child(By.Type)]` — by type) |
| `[Parent]` | A parent node of the required type |
| `[SceneService]` | A service from the nearest `IServiceProvider` up the tree (that is, from `World`) |
| `[Logger]` | A `Serilog.ILogger` configured for the current class |
| `[NotNull]` | A check that an `[Export]` field is filled in in the editor (in `CheckedAbstractStorage`) |

> [!IMPORTANT]
> Without `Di.Process(this)` all the annotated fields silently stay `null` — the compiler will not
> catch this. `[Child]` matches **by field name**, so renaming a field requires renaming the node in
> the scene (and vice versa).

`CheckedAbstractStorage` is the base for all the `PackedScene` storages (`RootPackedScenes`,
`GamePackedScenes`, `SyncedPackedScenes`, `ClientPackedScenes`, `PagesProvider`). The references to
the scene prototypes are configured in the Godot editor; obtaining any scene for instantiation starts
here, not with `GD.Load`.
