# Asset Renamer

Editor tool to batch-rename project assets to a consistent naming convention. Built as a teaching aid for asset naming conventions, usable on any asset type (textures, models, prefabs, VFX, materials, audio...).

## Install (Unity Package Manager, git URL)

Window > Package Manager > + > "Install package from git URL":

```
https://github.com/ZellTBH/AssetRenamer.git
```

Pin a version by appending its tag to the URL, e.g. `#0.5.1`.

## Usage

1. Open via `Breaking Frame > Asset Renamer`.
2. Drag assets from the Project window onto the drop zone, or select them and click **Add selection**. Folders are expanded to their contents.
3. Pick a naming convention (click **?** to preview every style). Optionally enable normalization, the type prefix and the Transformations (find / replace, custom prefix and suffix).
4. Review the preview. Each row shows the old and new name with a status pill. Collisions and invalid names are flagged and skipped; untick a row to exclude it.
5. Click **Apply**. Renames go through `AssetDatabase`, so GUIDs and references are preserved. **Revert** undoes the last applied batch.

## Type prefixes

Prefixes are resolved by an `AssetTypePrefixTable` asset (create or load one with the **Create or Load Table** button, or via `Assets > Create > Asset Renamer > Type Prefix Table`). Extension rules are evaluated before type rules. The prefix keeps its own `_` separator and is never re-cased.

## Naming conventions

Pascal, camel, snake, kebab, flat, upper flat, Pascal snake, camel snake, screaming snake, train, cobol. Some (cobol, upper flat) are included for completeness and rarely fit assets.

## Tests

EditMode tests live in `Tests/Editor`. In the host project, list the package under `"testables"` in `Packages/manifest.json`, then run them from the Test Runner (`AssetRenamer.Editor.Tests`).
