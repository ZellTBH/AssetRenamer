# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.5.3] - 2026-10-25

### Changed
- The README is rewritten and is now the only documentation of the package. Every fact in it is checked against the code: default prefixes, convention examples, preview statuses, the Update button and network access.
- `documentationUrl` points to the README, so the Package Manager "Documentation" link opens it.

### Removed
- The PDF quick guide (`Documentation~/AssetRenamer.pdf`): it showed the old interface and the old URL, and wrongly claimed the tool made no network access. The unused banner and icon art (`Documentation~/art/`) is removed with it.

### Fixed
- The Convention field has the theme style again (dark background, border-strong frame, rounded corners, accent frame on focus). It had lost it in 0.5.0 when the field rule was narrowed to stop framing the TRANSFORMATIONS foldout header: the enum field uses its own input class, which the narrowed rule did not list.

## [0.5.2] - 2026-10-25

### Changed
- The package is published under the Breaking Frame author, so the Package Manager lists it in the "Packages - Breaking Frame" group next to Header Forge. Package name, install URL and namespace are unchanged.

## [0.5.1] - 2026-10-25

### Fixed
- The install URL and the update check point to the renamed repository `ZellTBH/AssetRenamer` (the old address only worked through a GitHub redirect).

## [0.5.0] - 2026-10-25

### Added
- EditMode test suite (106 tests): characterization of the tokenizer, formatter, normalizer, redundant type word filter and engine plans, plus the prefix table helper. Run them from the Test Runner once the package is listed under `"testables"` in the host manifest.
- Status pill on every preview row (colored dot plus status word). A message line appears only for Collision and Invalid.
- The drop zone lights up while assets are dragged over it.
- `Documentation~/wcag_check.py`: checks the contrast of every color pair used by the stylesheet, composing translucent colors over the background they sit on.

### Changed
- Breaking Frame visual language, aligned on the other forges: header with the Breaking Frame logo and the tool title, toolbar (Add selection, Clear, Revert, Apply), translucent rounded sections (Convention, Prefix, Transformations, Preview), footer with counts, version and the Update badge.
- The window layout is a UXML file. Options are built once and only the preview refreshes on change, so text fields keep focus and the scroll position stays put.
- Colors come from a single token block in the stylesheet, with one status palette: Collision is now yellow and Invalid red (the code and the stylesheet used to disagree and swapped them).
- Contrast meets WCAG 2.2 AA: no blue text below 4.5:1, a visible frame on every input field and checkbox, minimum text size 11px.
- Apply, Clear and Revert are disabled when there is nothing to do; Revert stays visible.
- "Create Default Table" is now "Create or Load Table".
- The stylesheet moved to `Editor/Resources`.

### Fixed
- "Create Default Table" no longer writes over an existing asset at `Assets/AssetTypePrefixTable.asset`. Before, an existing prefix table lost all its custom rules (reset to the defaults), and an asset of another type at that path was turned into a prefix table, silently redirecting every reference to it. The button now loads the existing table, and logs an error without touching anything when another asset type occupies the path.

### Removed
- Embedded base64 banner art (`EmbeddedArt.cs`, about 1.78 MB).

## [0.4.0] - 2026-06-06

### Added
- Strip redundant type words: when a name already contains a word that just repeats its type prefix, it is dropped (e.g. wall_mat -> M_Wall instead of M_WallMat, Wall Prefab -> PF_Wall, TorchPF -> PF_Torch). A glued upper-case acronym tail is detached first so the tokenizer can separate it. Controlled by a "Strip redundant type words" toggle, on by default.
- PrefixRule gains an optional aliases field listing the words that mean the same type (e.g. "material mat" for M_), so each studio can tune what counts as redundant.
- A "Reset table to defaults" button on the tool to refresh an existing prefix table with the up-to-date built-in rules (so an older table picks up the new type aliases after upgrading).

### Changed
- Prefix table resolution refactored around a single TryResolveRule, so the prefix and its aliases come from the same matched rule (no behavior change for existing prefixes).

## [0.3.1] - 2026-06-06

### Changed
- Update confirmation now shows the current and target versions (e.g. "Update from v0.3.0 to v0.3.1?").

## [0.3.0] - 2026-06-06

### Added
- Find & replace: a case-insensitive search/replace applied to each name before formatting.
- Custom prefix and custom suffix: free text wrapped around the formatted name, written verbatim.
- Collapsible "Find / Replace / Affixes" section, collapsed by default, marked with an indicator when a transform is active.
- Version readout in the footer, plus an "Update available" button that appears only when a newer version exists on the repository (reads the manifest on main once per session; git installs only).

### Changed
- Engine formatting options bundled into a RenameOptions struct (internal refactor, no behavior change).

## [0.2.0] - 2026-06-05

### Added
- Number suffix padding: pad a trailing number to a fixed width (1 / 01 / 001) or Auto, which aligns every number to the widest one in the batch.
- Convention help panel: a "?" button next to the convention dropdown previews every convention with a live example, highlighting the active one.
- Tooltips on every control.
- Revert: undo the last applied batch in one click.
- Folder drop: dropping a folder adds all its contents recursively.
- Add selection: add the assets currently selected in the Project window.
- Hide unchanged in preview, plus a per-row toggle to exclude individual assets from the batch (Excluded status).
- documentationUrl in the package manifest so the Package Manager "Documentation" link opens the guide.

### Changed
- Apply now surfaces failed renames (count + Console error) instead of swallowing them, and shows a result line.
- Drag-and-drop is restricted to assets under Assets/ (dropping from immutable Packages/ was a silent no-op before).

## [0.1.0] - 2026-06-05

### Added
- Initial release of the Asset Renamer editor tool.
- Drag-and-drop batch renaming of project assets to a chosen naming convention (Pascal, camel, snake, kebab, flat, upper flat, Pascal snake, camel snake, screaming snake, train, cobol).
- Optional name normalization: strips diacritics and copy markers ("(1)", "copie"/"copy") before formatting.
- Optional auto type prefix driven by an editable `AssetTypePrefixTable` ScriptableObject (extension rules first, then type rules by inheritance).
- Preview with per-asset status (Ok, Unchanged, Collision, Invalid) and in-batch collision detection.
- Renaming goes through `AssetDatabase`, preserving GUIDs and references.
