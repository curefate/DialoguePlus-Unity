# DialoguePlus for Unity

Unity integration for **DialoguePlus.Core**.

- Import `.dp` files as `DialoguePlusScript` assets.
- Execute scripts via `DialoguePlusAdapter`.
- Resolve `import` statements (supports relative imports for `addr://` scripts).
- Bind C# methods callable from scripts via `[DPFunction]`.

## Install

Add this package via Unity Package Manager (UPM).

Git URL (Package Manager → **+** → *Add package from git URL...*):

```text
https://github.com/curefate/DialoguePlus-Unity.git
```

This package expects **Unity Addressables** to be installed.

## Quick Start

### 1) Import a script

1. Create a file, e.g. `Assets/DialoguePlusScripts/main.dp`.
2. Unity imports it as a `DialoguePlusScript` asset.

### 2) Configure RootFolder

Ensure you have `Assets/DialoguePlusSettings.asset`.

- `RootFolder` is used to compute a runtime `SourceId` (see **Advanced**).
- Default is `Assets/`.

### 3) Add `DialoguePlusAdapter` to the scene

Add **exactly one** `DialoguePlusAdapter` component to a GameObject in your scene.

### 4) Bind required UI callbacks

Before executing, you must bind:

- `DialoguePlusAdapter.Instance.Executor.OnDialogueAsync`
- `DialoguePlusAdapter.Instance.Executor.OnMenuAsync`

These callbacks are how DialoguePlus asks your game to display dialogue and menus.
For a working reference, see the Sample: `Samples~/DemoScene/Scripts/ChatManager.cs`.

Minimal skeleton:

```csharp
using DialoguePlus.Core;
using System.Threading.Tasks;
using UnityEngine;

public sealed class DialogueUIBinder : MonoBehaviour
{
    private void Start()
    {
        var executor = DialoguePlusAdapter.Instance.Executor;

        executor.OnDialogueAsync = async (runtime, dialogue) =>
        {
            // TODO: show dialogue.Speaker + dialogue.Text.Evaluate(runtime) in your UI
            await Task.Yield();
        };

        executor.OnMenuAsync = async (runtime, menu) =>
        {
            // TODO: show options, wait for user selection
            // must return selected index (0-based)
            await Task.Yield();
            return 0;
        };
    }
}
```

### 5) Execute

Run from a `DialoguePlusScript` asset:

```csharp
await DialoguePlusAdapter.Instance.ExecuteToEnd(dialoguePlusScriptAsset);
```

Or run by `SourceId`:

```csharp
await DialoguePlusAdapter.Instance.ExecuteToEnd("addr://DialoguePlusScripts/main.dp");
```

## Binding C# functions (`[DPFunction]`)

Mark methods with `[DPFunction]` to make them callable from `.dp` scripts.

- Supported types: `string`, `bool`, `int`, `float` (params and return)
- Return type can also be `void`

Instance methods: the **first argument in script is the GameObject name** (used to find the component instance).

## Sample

Import the included Sample from Unity Package Manager:

- `ChatManager.cs` shows `OnDialogueAsync` / `OnMenuAsync` binding and a simple UI.

## Troubleshooting

- **`DialoguePlusAdapter.Instance is null`**: add a `DialoguePlusAdapter` component to the scene (exactly one).
- **`DialoguePlusScript is missing SourceId`**: the `.dp` file is likely outside `RootFolder` (see **Advanced**), or not imported as expected.
- **Addressables failed to load `DialoguePlusScript`**: check that the Addressables key matches the expected mapping (see **Advanced**).
- **Import target not found (import warnings)**: verify relative `import` paths.

## Advanced: `SourceId`, Addressables keys, and imports

DialoguePlus uses `SourceId` as the canonical identifier.

For Addressables scripts:

- `SourceId = "addr://" + <key>`

Key derivation:

- `RootFolder` comes from `DialoguePlusSettings`.
- `<key>` is the asset path relative to `RootFolder`, normalized to `/`, and includes `.dp`.

Example:

- Asset path: `Assets/DialoguePlusScripts/main.dp`
- RootFolder: `Assets/`
- Key: `DialoguePlusScripts/main.dp`
- SourceId: `addr://DialoguePlusScripts/main.dp`

Import rules:

- `import "addr://..."` is treated as absolute.
- Other absolute URIs are treated as already-resolved.
- Relative imports resolve relative to the current script key directory, then normalize (`./`, `../`).

## License

MIT License. See `LICENSE`.
