# Manifest Editor -- Design Spec

**Date:** 2026-03-29
**Status:** Approved

## Overview

A cross-platform desktop GUI for creating and editing `manifest.json` files used by the ModelPublisher CLI. Built with Avalonia UI so it runs on Windows, macOS, and Linux without changes.

---

## Project Structure

New project added to the existing solution:

```
ModelPublisher.sln
  src/
    ModelPublisher.Core/               (existing)
    ModelPublisher.Cli/                (existing)
    ModelPublisher.ManifestEditor/     (new)
      App.axaml / App.axaml.cs
      Views/
        MainWindow.axaml/.cs
        BasicInfoView.axaml/.cs
        FilesView.axaml/.cs
        DescriptionView.axaml/.cs
        PlatformsView.axaml/.cs
      ViewModels/
        MainWindowViewModel.cs         -- sidebar nav, open/save commands, dirty state
        BasicInfoViewModel.cs
        FilesViewModel.cs
        DescriptionViewModel.cs
        PlatformsViewModel.cs
        PlatformEntryViewModel.cs      -- one per platform: IsEnabled + config fields
      Models/
        ManifestEditorState.cs         -- maps to/from ReleaseManifest for editing
```

`ModelPublisher.ManifestEditor` references `ModelPublisher.Core` to reuse `ReleaseManifest`, `ManifestFiles`, `PlatformConfig`, `PatreonConfig`, etc. No model duplication.

**Dependencies:**
- `Avalonia` (UI framework)
- `CommunityToolkit.Mvvm` (source-generated MVVM: `[ObservableProperty]`, `[RelayCommand]`)

---

## Architecture

### MVVM

CommunityToolkit.Mvvm throughout. `[ObservableProperty]` generates property boilerplate at compile time. `[RelayCommand]` wires up button commands. No reactive streams.

### State

`ManifestEditorState` is the in-memory editing model. It holds all form data as flat, bindable properties and contains `ToManifest()` / `FromManifest(ReleaseManifest, string directory)` methods to convert to and from `ReleaseManifest` for serialization.

---

## UI Layout

**Shell:** Single window with a fixed left sidebar and a content area on the right.

**Sidebar contains:**
- Filename label (e.g. `manifest.json` or `untitled`)
- Four nav items: Basic Info, Files, Description, Platforms
- Open button (bottom)
- Save button (bottom, disabled when invalid)

**Content area** renders the active section's view.

**Dirty state:** Window title shows `*` when there are unsaved changes. Closing with unsaved changes shows a "Save changes?" confirm dialog.

---

## Sections

### Basic Info
- **Title** -- text field (required)
- **Tags** -- chip-style input: existing tags shown as removable chips, text field to add new ones (press Enter to add)
- **License** -- dropdown, pre-populated with common SPDX identifiers (CC-BY-4.0, CC-BY-SA-4.0, CC-BY-NC-4.0, MIT, GPL-3.0, etc.); defaults to CC-BY-4.0

### Files
- **Model files** -- list of file paths with up/down reorder buttons and a remove button per item, plus an "Add file..." button that opens a native file picker defaulting to the manifest directory
- **Photos** -- same list pattern as model files, with up/down reorder buttons (order matters -- Printables treats the last-uploaded photo as the cover)
- **Cover photo** -- optional dropdown populated from the photos list; empty option = "none (use first photo)"
- All paths stored relative to the manifest directory; shown as absolute paths in the UI for readability

### Description
- Full-height text editor (plain text, monospace font)
- Markdown is written verbatim -- the editor makes no attempt to parse or render it
- Live preview (split pane with rendered markdown) is a nice-to-have, deferred from v1

### Platforms
Six platform entries listed vertically, each as a toggle row:

| Platform | Key | Extra fields when enabled |
|----------|-----|---------------------------|
| Printables | `printables` | Tier (free/premium), Print profiles (file list) |
| MakerWorld | `makerworld` | Tier, Print profiles |
| Cults3D | `cults3d` | Tier, Print profiles |
| Thangs | `thangs` | Tier, Print profiles |
| MakerOnline | `makeronline` | Tier, Print profiles |
| Patreon | `patreon` | Tier, Free post (checkbox), Access tier ID (text field) |

When a platform is toggled **off**, it is excluded from the serialized `platforms` dictionary entirely.

When **on**, its row expands inline to show its config fields.

---

## Open / Save Flow

### Single entry point (Open button)
The Open button accepts two inputs:
- **A folder** -- if the folder already contains a `manifest.json`, loads it (same as picking the file directly); otherwise creates a blank manifest targeting that directory, saves to `{dir}/manifest.json`
- **An existing `manifest.json` file** -- loads and populates all form fields from the file; the file's parent directory becomes the manifest directory

This single path covers both create and edit workflows.

### Save
- Always writes to `{manifestDirectory}/manifest.json`
- File paths are serialized relative to the manifest directory (e.g. `./model.3mf`)
- No "Save As" -- the manifest is always co-located with the model files

### New (no Open yet)
The app starts in an untitled blank state. The user must click Open and pick a folder before they can save.

---

## Validation (on Save)

All checks run when the Save button is clicked. If any fail, a dialog lists all issues and the file is not written.

1. **Title** -- must be non-empty
2. **Model files** -- at least one file must be specified
3. **File existence** -- every path in models, photos, and print profiles must exist on disk
4. **Patreon access_tier_id** -- required when Patreon is enabled and `free_post` is false

**Opening a manifest with missing files:** paths that no longer exist are shown in red with a warning icon in the Files section. Editing is not blocked -- the user can fix or remove them.

**JSON parse error on open:** shown as an error dialog; the app stays in its current state.

---

## Out of Scope (v1)

- Markdown live preview
- Drag-and-drop file reordering (up/down buttons are in v1)
- CLI integration (launch publish from the editor)
- In-app platform status / publish history