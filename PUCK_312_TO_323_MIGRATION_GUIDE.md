## Puck B312 → B323 Mod Migration Guide

Scope: changes that affect mod authors. The B323 release contains a large mod-system refactor that reshapes the plugin contract, the `Mod` class, identifiers, events, and the server-side mod config. Most mods will not compile against B323 without changes.

Dump references:
- B312 source dump: `C:\PuckModdingTools\b312-full-dump\Scripts\Puck\`
- B323 source dump: `C:\PuckModdingTools\b323-full-dump\Puck\`

---

### TL;DR — the five things you must change

1. Implement `IPuckPlugin` instead of `IPuckMod` (interface rename, same two methods).
2. Mod IDs are now `string` (Steam Workshop string IDs) instead of `ulong`. Update every place you stored or compared mod IDs.
3. `Mod` no longer wraps `InstalledItem` — it wraps `SteamWorkshopItem`. `mod.InstalledItem.Id` becomes `mod.Id`, `mod.InstalledItem.ItemDetails.Title` becomes `mod.SteamWorkshopItem.Details.Title`, etc.
4. `ModManager` is now a `static` class (was a `MonoBehaviourSingleton<ModManager>`). All access becomes `ModManager.Foo` instead of `ModManager.Instance.Foo`.
5. Mod lifecycle events were renamed / reshaped. `Event_OnModEnableSucceeded` / `Event_OnModDisableSucceeded` are gone — listen to `Event_OnModStateChanged` instead. `isManual` is no longer included.

---

### 1. Interface rename: `IPuckMod` → `IPuckPlugin`

B312:
```csharp
public interface IPuckMod
{
    bool OnEnable();
    bool OnDisable();
}
```

B323:
```csharp
public interface IPuckPlugin
{
    bool OnEnable();
    bool OnDisable();
}
```

The method signatures and semantics are unchanged. The loader still reflects on your DLL for `IsClass && !IsAbstract && typeof(IPuckPlugin).IsAssignableFrom(type)`, instantiates via `Activator.CreateInstance`, and calls `OnEnable` / `OnDisable`. Returning `false` from either still triggers the failure path.

**Action:** rename the interface reference. If your DLL previously implemented `IPuckMod`, the new loader looks for `IPuckPlugin` and will throw `"IPuckPlugin missing from assembly"` if you leave the old interface in place.

---

### 2. ID type change: `ulong` → `string`

Throughout the mod system, B312's `ulong` Steam Workshop IDs are now `string` IDs.

| B312 | B323 |
|---|---|
| `ulong InstalledItem.Id` | `string SteamWorkshopItem.Id` |
| `Mod.InstalledItem.Id` (ulong) | `Mod.Id` (string) — convenience getter on `Mod` |
| `Dictionary<ulong, bool> ModsState` | `Dictionary<string, bool> ModsStatus` |
| `ulong[] InstalledModIds / EnabledModIds / DisabledModIds` | `Mod[] ReadyMods`, `Mod[] EnabledMods` (and `Plugin[] ReadyPlugins`, `Plugin[] EnabledPlugins`) |
| `ModConfig.id` is `ulong` | `ModConfig.id` is `string` |
| `GetModById(ulong id)` | `GetModById(string id)` |

If you persisted Steam Workshop IDs as `ulong` anywhere (save files, network payloads, dictionary keys), they need to be converted to `string`.

---

### 3. `Mod` class rewrite

B312's `Mod` was a self-contained class that implemented `INotifyPropertyChanged`, owned its own assembly loading, and exposed `IsEnabled` / `IsPlugin` / `IsAssemblyMod` setters.

B323 introduces a `BasePlugin<TState>` abstract base. Both `Mod` and the new `Plugin` class extend it, sharing all assembly load / enable / disable logic.

```csharp
// B323
public abstract class BasePlugin<T> where T : BasePluginState, new()
{
    public T State { get; set; }
    public string Path { get; }     // from state
    public bool IsReady { get; }    // from state
    public bool IsEnabled { get; }  // from state
    public bool HasAssembly { get; }

    public virtual void Initialize();
    public virtual void Dispose();
    public bool Enable();   // returns false if !IsReady or already enabled
    public bool Disable();
    public virtual void OnEnableFailed(Exception);
    public virtual void OnDisableFailed(Exception);
    protected virtual void OnStateChanged(T oldState, T newState);
}

public class Mod : BasePlugin<BasePluginState>
{
    public readonly SteamWorkshopItem SteamWorkshopItem;
    public string Id => SteamWorkshopItem.Id;
    // ...
}
```

Key behavioral differences:

- **State is immutable per snapshot.** Changes happen through `SetState(Dictionary<string,object> updates)` which produces a new `BasePluginState { Path, IsReady, IsEnabled }`. `OnStateChanged(old, new)` fires when any field actually changes. Do NOT mutate `mod.State` directly — assign a new state via the manager-driven flow.
- **`IsReady` is new and is required before enabling.** `Enable()` short-circuits and returns `false` if `IsReady == false`. A `Mod` becomes ready when its underlying `SteamWorkshopItem.Phase == SteamWorkshopItemPhase.Installed`. This is the new "download first, enable last" sequence the dev mentioned.
- **`IsPlugin` / `IsAssemblyMod` are gone.** "Plugin" is now a separate type (`Plugin` class, loaded from the `/Plugins` directory). Assembly-vs-asset detection is now `HasAssembly` (computed from `Path`).
- **`PreviewTexture` moved off `Mod`.** It now lives on `SteamWorkshopItemDetails.PreviewTexture` (downloaded by `SteamWorkshopItemDetails.Initialize()`, not by the Mod itself).
- **`INotifyPropertyChanged` is gone.** Replace property-changed subscriptions with the event-bus events (`Event_OnModStateChanged`, etc.) or the state-changed delegate on `SteamWorkshopItem.StateChanged` / `DetailsStateChanged`.
- **`Enable()` is now parameterless** and returns `bool` (B312 had `Enable(bool isManual = false)` returning `void`).

---

### 4. `InstalledItem` / `ItemDetails` removed → `SteamWorkshopItem` / `SteamWorkshopItemDetails`

Both classes were removed. Replacements:

| B312 | B323 |
|---|---|
| `InstalledItem` | `SteamWorkshopItem` |
| `InstalledItem.Id` (ulong) | `SteamWorkshopItem.Id` (string) |
| `InstalledItem.Path` | `SteamWorkshopItem.Path` (via state) |
| `InstalledItem.ItemDetails` | `SteamWorkshopItem.Details` |
| `ItemDetails.Title` | `SteamWorkshopItemDetails.Title` |
| `ItemDetails.Description` | `SteamWorkshopItemDetails.Description` |
| `ItemDetails.PreviewUrl` | `SteamWorkshopItemDetails.PreviewUrl` |
| `ItemDetails.Metadata` | `SteamWorkshopItemDetails.Metadata` |
| *(not present)* | `SteamWorkshopItemDetails.Subscriptions` (int, new) |
| *(not present)* | `SteamWorkshopItemDetails.Upvotes` (int, new) |
| *(not present)* | `SteamWorkshopItemDetails.Downvotes` (int, new) |
| `Mod.PreviewTexture` | `SteamWorkshopItemDetails.PreviewTexture` |
| `Event_OnItemDetails` listener on `InstalledItem` | Now internal to `SteamWorkshopItem`; subscribe to its `DetailsStateChanged` delegate or to `Event_OnModSteamWorkshopItemDetailsStateChanged` |

Phase is also new — `SteamWorkshopItemPhase { None, Downloading, Updating, Installed }`. Inspect via `mod.SteamWorkshopItem.Phase`.

---

### 5. `ModManager` is now `static`

B312:
```csharp
public class ModManager : MonoBehaviourSingleton<ModManager>
{
    public List<Mod> Mods;
    public Dictionary<ulong, bool> ModsState;
    public List<PendingMod> pendingMods;
    public void AddMod(InstalledItem item, bool isPlugin = false);
    public Mod GetModById(ulong id);
    public bool GetModState(ulong id);
    public void SetModState(ulong id, bool state);
    public void SetPendingMods(ulong[] ids);
    public void ResetPendingMods(string reason = null);
    public void SetModsToState();
    // ...
}
```

B323:
```csharp
public static class ModManager
{
    public static List<Plugin> Plugins;           // local /Plugins folder
    public static List<Mod> Mods;                  // Steam Workshop mods
    public static Dictionary<string, bool> ModsStatus;

    public static Plugin[] ReadyPlugins   { get; }
    public static Plugin[] EnabledPlugins { get; }
    public static Mod[]    ReadyMods      { get; }
    public static Mod[]    EnabledMods    { get; }

    public static Plugin AddPlugin(string id, string path);
    public static Plugin RemovePlugin(string id);
    public static Mod    AddMod(SteamWorkshopItem item);
    public static Mod    RemoveMod(string id);

    public static Plugin GetPluginById(string id);
    public static Mod    GetModById(string id);

    public static bool GetModStatus(string id);
    public static void SetModStatus(string id, bool isEnabled);
    public static void LoadModsStatus();
    public static void ApplyModStatus();
}
```

| B312 call | B323 call |
|---|---|
| `ModManager.Instance.Mods` | `ModManager.Mods` (Steam mods) + `ModManager.Plugins` (local) |
| `ModManager.Instance.GetModById(ulongId)` | `ModManager.GetModById(stringId)` |
| `ModManager.Instance.GetModState(id)` | `ModManager.GetModStatus(id)` *(note: state → status)* |
| `ModManager.Instance.SetModState(id, b)` | `ModManager.SetModStatus(id, b)` |
| `ModManager.Instance.LoadModsState()` | `ModManager.LoadModsStatus()` |
| `ModManager.Instance.SetModsToState()` | `ModManager.ApplyModStatus()` |
| `ModManager.Instance.EnabledModIds` | `ModManager.EnabledMods.Select(m => m.Id)` |
| `ModManager.Instance.SetPendingMods(ids)` | Replaced by `ReconnectionState` (see §7) |
| `ModManager.Instance.ResetPendingMods(reason)` | Replaced by `ReconnectionState` |

Persisted save key also renamed: `modsState` → `modsStatus`. Old save data will not be read.

Plugins (local DLLs in the `/Plugins` directory) are now a separate concept — `LoadPlugins()` creates `Plugin` instances (not `Mod` instances flagged with `IsPlugin = true`). If you previously dropped your DLL into `/Plugins`, you are now technically a `Plugin`, not a `Mod`. The loader code path is the same `BasePlugin<T>` machinery, so your `IPuckPlugin` implementation still works the same way.

---

### 6. Event renames & shape changes

Removed events (no longer fired):

- `Event_OnModEnableSucceeded`
- `Event_OnModDisableSucceeded`
- `Event_OnModChanged`
- `Event_OnBeforePendingModsSet`
- `Event_OnPendingModsSet`
- `Event_OnPendingModRemoved`
- `Event_OnPendingModsReset`
- `Event_OnPendingModsCleared`
- `Event_OnItemDetails` (still triggered internally for SteamWorkshopItem, but you should consume the higher-level events)

Still present:

- `Event_OnModEnableFailed` — payload now `{ "mod" }` only (no `"isManual"`)
- `Event_OnModDisableFailed` — payload now `{ "mod" }` only
- `Event_OnModAdded` — payload `{ "mod" }`
- `Event_OnModRemoved` — payload `{ "mod" }`

New events:

| Event | Payload | When it fires |
|---|---|---|
| `Event_OnModStateChanged` | `{ "mod", "oldState", "newState" }` (states are `BasePluginState`) | Any change to `Path` / `IsReady` / `IsEnabled` on a mod |
| `Event_OnPluginStateChanged` | `{ "plugin", "oldState", "newState" }` | Same, for local Plugins |
| `Event_OnModSteamWorkshopItemStateChanged` | `{ "mod", "oldState", "newState" }` (`SteamWorkshopItemState`) | Underlying Workshop item path/phase/details swapped |
| `Event_OnModSteamWorkshopItemDetailsStateChanged` | `{ "mod", "oldState", "newState" }` (`SteamWorkshopItemDetailsState`) | Title/description/preview/votes/metadata change |
| `Event_OnPluginAdded` / `Event_OnPluginRemoved` | `{ "plugin" }` | Local plugin add/remove |
| `Event_OnPluginEnableFailed` / `Event_OnPluginDisableFailed` | `{ "plugin" }` | Plugin lifecycle failure |
| `Event_OnModsModEnabled` / `Event_OnModsModDisabled` | `{ "mod" }` | UI request to toggle a mod — fired by the mod window, consumed by `ModManagerController` |
| `Event_OnModsPluginEnabled` / `Event_OnModsPluginDisabled` | `{ "plugin" }` | UI request to toggle a plugin |

**Migration pattern for "I want to know when a mod became enabled":**

```csharp
// B312
EventManager.AddEventListener("Event_OnModEnableSucceeded", data => {
    var mod = (Mod)data["mod"];
    // ...
});

// B323
EventManager.AddEventListener("Event_OnModStateChanged", data => {
    var mod = (Mod)data["mod"];
    var oldState = (BasePluginState)data["oldState"];
    var newState = (BasePluginState)data["newState"];
    if (!oldState.IsEnabled && newState.IsEnabled) {
        // mod just became enabled
    }
});
```

---

### 7. `PendingMod` removed → `ReconnectionState`

The `PendingMod` class and the `ModManager.pendingMods` list are gone. The "you need to install these mods before you can join this server" flow now lives in `GlobalStateManager.ReconnectionState`:

```csharp
public struct ReconnectionState
{
    public ReconnectionPhase Phase;             // None, AwaitingMods, ...
    public string Password;
    public string[] ClientRequiredModIds;
    public string[] PendingReadinessModIds;     // not yet installed
    public string[] PendingEnablingModIds;      // installed but not yet enabled
    public string[] PendingModIds { get; }      // union of the two
    public bool IsPendingModId(string modId);
}
```

UI flow:
- `ConnectionPhase.Disconnected` triggers `ModManager.ApplyModStatus()` to restore user state.
- Missing mods on connect surface through the new `PopupMissingModsPopupContent` popup.
- The "download first, enable last" sequence is driven by `Event_OnReconnectionStateChanged` and the `ReconnectionPhase.AwaitingMods` phase in `ModManagerController.Event_OnReconnectionStateChanged`.

If your mod previously listened to `Event_OnPendingModsSet` to react to "the user wants to join a server that requires these mods", listen to `Event_OnReconnectionStateChanged` and check `newReconnectionState.Phase == ReconnectionPhase.AwaitingMods`.

---

### 8. `ModConfig` (server config) field renames

| B312 | B323 |
|---|---|
| `ulong id` | `string id` |
| `bool enabled` | `bool isEnabled` |
| `bool clientRequired` | `bool isClientRequired` |

`ServerConfig.mods` is still a `ModConfig[]`. Any server config JSON / serialization you do for required-mods needs the field renames and the ID type change.

`ServerConfig` exposes helpers:
```csharp
serverConfig.mods                                 // ModConfig[]
serverConfig.modIds          // (string[]) where isEnabled
serverConfig.clientRequiredModIds                 // (string[]) where isClientRequired
```

---

### 9. Quick translation table

| B312 expression | B323 expression |
|---|---|
| `typeof(IPuckMod)` | `typeof(IPuckPlugin)` |
| `mod.InstalledItem.Id` | `mod.Id` |
| `mod.InstalledItem.Path` | `mod.Path` |
| `mod.InstalledItem.ItemDetails.Title` | `mod.SteamWorkshopItem.Details.Title` |
| `mod.InstalledItem.ItemDetails.PreviewUrl` | `mod.SteamWorkshopItem.Details.PreviewUrl` |
| `mod.PreviewTexture` | `mod.SteamWorkshopItem.Details.PreviewTexture` |
| `mod.IsEnabled = true` | `mod.Enable()` (returns bool; only works if `mod.IsReady`) |
| `mod.IsPlugin` | check whether the object is in `ModManager.Plugins` vs `ModManager.Mods` (or `is Plugin`) |
| `mod.IsAssemblyMod` | `mod.HasAssembly` |
| `ModManager.Instance.Mods` | `ModManager.Mods` (+ `ModManager.Plugins`) |
| `ModManager.Instance.GetModById(123UL)` | `ModManager.GetModById("123")` |
| `ModManager.Instance.GetModState(id)` | `ModManager.GetModStatus(id)` |
| `ModManager.Instance.SetModState(id, b)` | `ModManager.SetModStatus(id, b)` |
| `ModManager.Instance.SetModsToState()` | `ModManager.ApplyModStatus()` |
| `Event_OnModEnableSucceeded` | `Event_OnModStateChanged` (filter on `!old.IsEnabled && new.IsEnabled`) |
| `Event_OnModChanged` | `Event_OnModStateChanged` (and/or the two SteamWorkshopItem* state-changed events) |
| `Event_OnPendingModsSet` | `Event_OnReconnectionStateChanged` + check `Phase == AwaitingMods` |
| save key `"modsState"` | save key `"modsStatus"` |

---

### 10. Suggested migration order for an existing mod

1. Find/replace `IPuckMod` → `IPuckPlugin`. Build. The compiler will surface most of the remaining call sites.
2. Replace `ModManager.Instance.X` → `ModManager.X` everywhere.
3. Change every `ulong` mod ID to `string` (storage, dictionary keys, network payloads, save data).
4. Replace `mod.InstalledItem.*` accesses with `mod.Id` / `mod.Path` / `mod.SteamWorkshopItem.Details.*`.
5. Replace direct `mod.IsEnabled = ...` writes with `mod.Enable()` / `mod.Disable()` calls; gate on `mod.IsReady` where needed.
6. Re-wire any event listeners using the table in §6. Pay special attention to listeners on `Event_OnModEnableSucceeded` and `Event_OnModChanged` — they will silently never fire on B323.
7. If you read `ServerConfig.mods` or shipped a server JSON template, update field names (`enabled` → `isEnabled`, `clientRequired` → `isClientRequired`) and ID type.
8. If you tracked pending-mods state for the connect flow, switch to `GlobalStateManager.ReconnectionState`.
9. Rebuild against the B323 `Assembly-CSharp.dll`.
