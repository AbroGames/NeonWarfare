# Phase 2 — Known Servers Service (detailed)

**Epic:** #12 (MainMenu rebuild). **Phase:** 2 of 8. **Prerequisite for:** Phase 4 (`ServerListPage`). **Blocked by:** nothing (Phase 1 need not be merged, only built).

This plan adds a single persistence service for the user's saved/known multiplayer servers. **No UI is created here.** No pages, no locale keys, no removal of existing files. `ServerListPage` (Phase 4) is the consumer; it will call `Services.KnownServers.GetAll/Add/Remove/Exists`.

## Goal (done = all of)

1. Record `KnownServer(string Host, int? Port, string Label)` exists and is persistence-friendly (JSON-serializable by default options).
2. `KnownServersService` persists a `List<KnownServer>` to `user://known-servers.json`, mirroring the `GameSettingsService` / `ResumableGameService` shape.
3. Public API on the service: `Init()`, `GetAll()`, `Add(KnownServer)`, `Remove(KnownServer)`, `Exists(string host, int? port)`.
4. `Add` does **not** deduplicate (per spec); `Exists` is a separate query helper for callers.
5. Service is registered as `Services.KnownServers` and initialized in the **client** root starter only.
6. `dotnet build` succeeds with **0 errors**.

---

## Context you must know before editing

Read these facts. Do not assume otherwise.

- **Persistence-service shape (the pattern to mirror).** Two existing services already do exactly this "load-once, mutate, save" dance against `user://`. Copy their structure, not their field names:
  - `Scripts/Service/Settings/GameSettingsService.cs` — `private const string Path = "user://...json"; private T _field; Init(){ _field = Default(); Load(); } Get(); Set(); Save(){ FileAccess.Open(Write); file.StoreString(JsonSerializer.Serialize(...)); file.Close(); } Load(){ if(!FileAccess.FileExists){ Save(); return;} FileAccess.Open(Read); json=file.GetAsText(); _field = Deserialize(json); }`
  - `Scripts/Service/ResumableGame/ResumableGameService.cs` — same skeleton; also the closest analog because its data is a **record** in a **sibling file** in the **same folder**.
- **Folder = namespace convention.** `Scripts/Service/ResumableGame/` → namespace `NeonWarfare.Scripts.Service.ResumableGame`; `Scripts/Service/Settings/` → `NeonWarfare.Scripts.Service.Settings`. The new service follows the same rule: folder `Scripts/Service/KnownServers/` → namespace `NeonWarfare.Scripts.Service.KnownServers`.
  - The general plan named only `Scripts/Service/KnownServersService.cs`. We split into a **folder with two files** (`KnownServer.cs` + `KnownServersService.cs`) to mirror `ResumableGame/` exactly. This is an intentional, documented deviation from the single-file wording; it keeps the data record discoverable and matches the closest existing analog.
- **Records serialize with default `System.Text.Json` options.** `ResumableGame` and `GameSettings` are positional records and serialize fine with `JsonSerializer.Serialize(...)` (no custom options). A `List<KnownServer>` serializes the same way — `System.Text.Json` handles `List<T>` out of the box. **Do not** introduce a custom `JsonSerializerOptions` here.
- **Record value-equality is the removal mechanism.** `List<T>.Remove(item)` uses `Equals`, which for a positional record is structural. Two `KnownServer` instances with identical `Host`/`Port`/`Label` compare equal → `Remove` works without writing a custom comparer. This is why we use a record, not a class.
- **`Init()` is called manually, not by a DI container.** See `Scenes/Root/Starters/ClientRootStarter.cs` lines ~21–30: `Services.LastGame.Init();`, `Services.GameSettings.Init();`. The new service is wired the same way.
- **Client-only.** `Scenes/Root/Starters/DedicatedServerRootStarter.cs` only calls `Services.LastGame.Init()` (line ~21) — it does **not** init `GameSettings`. A dedicated server has no concept of a client's saved-server list. **Do not** add `KnownServers.Init()` to the dedicated server starter.
- **Godot file API used by the pattern:**
  - `FileAccess.FileExists(path)` — existence check.
  - `FileAccess.Open(path, FileAccess.ModeFlags.Write)` / `ModeFlags.Read` — opens; returns a disposable `FileAccess`.
  - `file.StoreString(json)` / `file.GetAsText()` — write/read text.
  - `file.Close()` — the existing code calls it explicitly inside a `using var file =` block; keep that habit for parity (harmless on a disposed handle path).
- **`int?` port is intentional.** The record mirrors `ResumableGame.Port` (`int?`) and matches `Services.MainScene.ConnectToMultiplayerGame(string host = null, int? port = null)`. A null port means "use the default". `Exists` therefore takes `int?` too, and compares with `==` (nullable equality is value equality for `int?`).

---

## Task 2.1 — Create the data record

**File (new):** `Scripts/Service/KnownServers/KnownServer.cs`

Minimal positional record. No static factory methods needed (unlike `ResumableGame`, there is no "None" sentinel — an empty list represents "no known servers").

```csharp
namespace NeonWarfare.Scripts.Service.KnownServers;

public record KnownServer(string Host, int? Port, string Label);
```

Notes:
- `Host` non-nullable by contract; callers must pass a non-empty host. The service does **not** validate emptiness (validation is a Phase 4 UI concern).
- `Port` nullable: `null` = "default port" (matches `ConnectToMultiplayerGame`).
- `Label` is a free-form user-facing name; may be empty string, never null. JSON round-trips empty strings fine.
- No `[Name]`/`[Hint]`/`[Category]` attributes — those belong to the *menu* settings layer (`MenuGameSettings`), not to this standalone persistence record.

---

## Task 2.2 — Create the service

**File (new):** `Scripts/Service/KnownServers/KnownServersService.cs`

Mirror `ResumableGameService` skeleton exactly (constants, single private field, `Init`/`GetAll`/mutators, private `Save`/`Load`). Differences: it holds a `List<KnownServer>` instead of a single record, and it exposes `Add`/`Remove`/`Exists`.

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

namespace NeonWarfare.Scripts.Service.KnownServers;

public class KnownServersService
{
    private const string KnownServersPath = "user://known-servers.json";

    private List<KnownServer> _servers;

    public void Init()
    {
        // Set default (empty list) before loading, mirroring GameSettingsService.Init.
        _servers = new List<KnownServer>();
        Load();
    }

    public List<KnownServer> GetAll()
    {
        return _servers;
    }

    public void Add(KnownServer server)
    {
        // No host:port dedupe by design (see plan). Callers use Exists() to check first if needed.
        _servers.Add(server);
        Save();
    }

    public void Remove(KnownServer server)
    {
        _servers.Remove(server);
        Save();
    }

    public bool Exists(string host, int? port)
    {
        return _servers.Any(server => server.Host == host && server.Port == port);
    }

    private void Save()
    {
        using var file = FileAccess.Open(KnownServersPath, FileAccess.ModeFlags.Write);
        string json = JsonSerializer.Serialize(GetAll());
        file.StoreString(json);
        file.Close();
    }

    private void Load()
    {
        if (!FileAccess.FileExists(KnownServersPath))
        {
            Save(); // persist the empty default so the file always exists afterwards
            return;
        }

        using var file = FileAccess.Open(KnownServersPath, FileAccess.ModeFlags.Read);
        string json = file.GetAsText();
        file.Close();

        _servers = JsonSerializer.Deserialize<List<KnownServer>>(json);
    }
}
```

Notes / decisions baked into this code:

- **`GetAll()` returns the live list, not a copy.** This matches `GetSettings()`/`GetLastGame()` which also return the internal reference (records are immutable-by-field, but the list itself is mutable). Phase 4's `ServerListPage` reads it for display and passes selected entries to `Remove`. If Phase 4 later needs snapshot isolation it can `.ToList()` at the call site; do not add a defensive copy now (no caller exists yet).
- **`Add` does not dedupe.** The general plan says "Без дедупликата host:port" explicitly. `Exists` exists precisely so the caller can decide ("auto-add after connect only if new"). Do not merge them.
- **`Remove` relies on record structural equality** — see Context. No custom `IEqualityComparer`.
- **No `[Logger]` field.** `ResumableGameService` and `GameSettingsService` have no logger either; `SaveLoadService` is the one that logs because it handles binary I/O errors. Keep parity: no logging here. If a future phase wants load-error logging, add it then.
- **No constructor / no `Di.Process(this)`.** This service has no `[Child]`/`[Logger]` injections, so it needs neither. `Services.cs` constructs it with `new KnownServersService()` (Task 2.3).
- **Deserialize target type is `List<KnownServer>`.** If the file is empty/corrupt, `JsonSerializer.Deserialize<List<KnownServer>>` throws (`JsonException`) — same failure mode as the existing services for their respective types. Do not add try/catch in Phase 2; match existing behavior. (If a later phase wants resilience against a corrupt file, that is a separate, project-wide change.)

---

## Task 2.3 — Register the service in `Services.cs`

**File:** `Scripts/Services.cs`

### 2.3a — Add `using`

Near the other `using NeonWarfare.Scripts.Service.*;` lines (currently lines 6–8), add:

```csharp
using NeonWarfare.Scripts.Service.KnownServers;
```

### 2.3b — Add the static field

In the `// Services from game` block (currently lines 32–40), add one line. Place it next to the other persistence services for readability — after `ResumableGameService LastGame`:

```csharp
public static readonly KnownServersService KnownServers = new();
```

Keep the existing block order otherwise unchanged.

---

## Task 2.4 — Initialize in the client root starter

**File:** `Scenes/Root/Starters/ClientRootStarter.cs`

Inside `Init(RootData rootData)` (currently lines 14–34), add the call. Place it alongside the other `Services.*.Init()` calls — after `Services.LastGame.Init();` (line 23) is the natural spot:

```csharp
Services.LastGame.Init();
Services.KnownServers.Init();
```

**Do not** add it to `DedicatedServerRootStarter.cs`. See Context ("Client-only").

---

## Task 2.5 — Verify

1. Confirm the two new files exist and compile:
   - `Scripts/Service/KnownServers/KnownServer.cs`
   - `Scripts/Service/KnownServers/KnownServersService.cs`
2. Confirm `Services.cs` references compile (the `using` and the field).
3. Confirm `ClientRootStarter.cs` references compile.
4. Build from the repository root:
   ```bash
   dotnet build
   ```
   Expect: **0 errors**. Warnings are acceptable but read them.
5. The implementer is **not** required to run the game in Phase 2. (Optional sanity check, not blocking: after a run, `user://known-servers.json` should be created on first client launch as an empty-ish JSON array `[]`. Do not block Phase 2 sign-off on this.)

---

## Out of scope for Phase 2 (do NOT do)

- Any page under `Pages/` — `ServerListPage` is Phase 4.
- Any locale key (`SERVER_LIST_MENU__*`) — Phase 8, or Phase 4 alongside its page.
- Removal of `ConnectPage` — Phase 4.
- Auto-adding the current connection to known servers — that is caller logic in Phase 4 (`ServerListPage.OnConnect`), not service logic.
- Validation of host format / port range — Phase 4 UI concern.
- Snapshot/defensive-copy semantics on `GetAll()` — defer to Phase 4 if a real need appears.
- A custom `JsonSerializerOptions` or `JsonConverter` for `KnownServer` — none needed.
- Initializing `KnownServers` on the dedicated server starter.

---

## Gotchas recap

- **Folder + 2 files**, not the single file named in the general plan — deliberate, to mirror `ResumableGame/` (record + service siblings). Documented above.
- **Client-only `Init()`.** Dedicated server never inits this service; do not touch `DedicatedServerRootStarter.cs`.
- **`Add` has no dedupe** — by spec. `Exists` is the separate query; do not fold them.
- **`Remove` uses record equality** — works because `KnownServer` is a positional record. No custom comparer.
- **`List<KnownServer>` serializes with default JSON options** — do not add custom options.
- **`Port` is `int?`** — matches `ResumableGame.Port` and `ConnectToMultiplayerGame`. `Exists` takes `int?` and compares with `==`.
- **Return the live list from `GetAll()`**, matching the existing services' "return the internal reference" convention; snapshot isolation, if ever needed, is the caller's job.
- **First run:** `Load()` writes the default empty list when the file is missing, so the file always exists after first init (same as `ResumableGameService.Load`).
