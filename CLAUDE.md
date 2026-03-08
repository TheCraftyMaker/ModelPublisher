# ModelPublisher — Claude Context

## What this project does
CLI tool that automates publishing 3D printable models to multiple platforms (Printables, MakerWorld, Cults3D, Thangs, MakerOnline, Patreon) using Playwright browser automation.

## Stack
- .NET 10, C#
- Playwright.NET — browser automation (Brave browser, headless=false)
- System.CommandLine 2.0.3 — use `SetAction` / `ParseResult.GetValue` (old Handler API is gone)
- Spectre.Console — always wrap user-data strings with `Markup.Escape()`, square brackets crash it

## Run the tool
```bash
cd C:\Source\ModelPublisher
dotnet run --project src\ModelPublisher.Cli -- "C:\Users\chris\Downloads\Models\<model-name>\manifest.json" --platforms printables
```

## Key source files
| File | Purpose |
|------|---------|
| `src/ModelPublisher.Cli/Program.cs` | CLI entry point |
| `src/ModelPublisher.Core/PublishCommand.cs` | Orchestrator — reads manifest, resolves tier, runs publishers, writes results JSON |
| `src/ModelPublisher.Core/Platforms/IPlatformPublisher.cs` | Publisher interface |
| `src/ModelPublisher.Core/Platforms/PrintablesPublisher.cs` | Only fully working publisher |
| `src/ModelPublisher.Core/Shared/BrowserContextFactory.cs` | Launches persistent Brave context per platform |
| `src/ModelPublisher.Core/Shared/AuthGuard.cs` | Pauses for human login when not authenticated |
| `src/ModelPublisher.Core/Shared/FileUploadHelper.cs` | `UploadSequentialAsync` — uploads one file at a time, waits for NetworkIdle |
| `src/ModelPublisher.Core/Shared/MarkdownHelper.cs` | `ToPlainText` and `ToTipTapHtml` — converts markdown for platforms that need it |
| `src/ModelPublisher.Core/Models/ReleaseManifest.cs` | Deserializes manifest.json |
| `src/ModelPublisher.Core/Models/PublishResult.cs` | Result record — `Tier` is set by orchestrator via `with`, not by publishers |

## Manifest format
```json
{
  "title": "...",
  "description": "... markdown ...",
  "tags": [],
  "license": "CC-BY-4.0",
  "files": {
    "models": ["./model.3mf"],
    "cover": "./cover-photo.jpg",
    "photos": ["./cover-photo.jpg", "./detail.jpg"]
  },
  "platforms": {
    "printables": { "tier": "free" }
  }
}
```
- `cover` is optional. If set, `PhotosOrdered(coverFirst)` deduplicates and positions it.
- `manifest.ManifestDirectory` is set after deserialization; use `ResolveFilePath()` for all file paths.

## Platform status
| Key | Platform | Status |
|-----|----------|--------|
| `printables` | Printables.com | **Working** |
| `makerworld` | MakerWorld.com | Stub — needs codegen selectors |
| `cults3d` | Cults3D.com | Stub — needs codegen selectors |
| `thangs` | Thangs.com | Stub — needs codegen selectors |
| `makeronline` | Maker.online | Stub — needs codegen selectors |
| `patreon` | Patreon.com | Stub — clipboard paste approach, needs verification |

## Printables — working selectors
- Model input: `input[type='file'][accept*='.3mf']` (also stl, obj, zip variants)
- Photo input: `input[type='file'][id='photos-upload-input']` — use `manifest.Files.PhotosOrdered(coverFirst: false)` (Printables uses last-uploaded as cover — though this is unconfirmed, see GitHub issue #5)
- Title: `GetByRole(AriaRole.Textbox, new() { Name = "Model name (required)" })`
- Summary: `GetByRole(AriaRole.Textbox, new() { Name = "Summary (required)" })`
- Tags: `GetByRole(AriaRole.Textbox, new() { Name = "Additional tags" })` + Enter per tag
- Original model: `GetByRole(AriaRole.Radio, new() { Name = "Original model – I made it" })`
- AI generated: `GetByRole(AriaRole.Radio, new() { Name = "No — fully human-made" })`
- Description: inject via `EvaluateAsync` — `MarkdownHelper.ToTipTapHtml(manifest.Description)` → set `el.innerHTML` → dispatch `input` + `change` events
- License: `GetByRole(AriaRole.Button, new() { Name = "License (required)" })`
- Submit: `GetByRole(AriaRole.Button, new() { Name = "Save draft" })` → wait for URL `**/model/**`
- Auth check: `[data-cy='user-menu'], .user-avatar, [aria-label='User menu']`

## Known gotchas
- **Printables description**: uses TipTap (ProseMirror). `FillAsync` doesn't work — use `EvaluateAsync` to set `innerHTML` directly.
- **MarkdownHelper.ToPlainText**: list markers replaced with `• ` / `1. ` (not stripped). Lists processed before bold/italic to avoid `*` bullets being consumed by the bold regex.
- **PublishResult.Tier**: init-only, set by orchestrator with `result with { Tier = tier! }`. Publishers do not set it.
- **Spectre.Console**: any string containing `[` or `]` from user data must be wrapped in `Markup.Escape()`.
- **System.CommandLine 2.0.3**: `SetAction` + `ParseResult.GetValue` only — old `Handler` API removed.

## GitHub workflow
- Repo: https://github.com/TheCraftyMaker/ModelPublisher
- `master` is protected — PRs required, no direct pushes, enforce_admins=true
- Auto-delete branches on merge is enabled
- `gh` CLI is NOT on bash PATH — call via: `powershell.exe -Command "& 'C:\Program Files\GitHub CLI\gh.exe' ..."`
- All commits must include: `Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>`
- See `.claude/skills/github-modelp/SKILL.md` for the full workflow reference

## Browser profiles
Persistent Chromium sessions stored in `profiles/{platformKey}/` relative to working directory. Gitignored — contains login cookies. On first run per platform, the browser pauses for manual login.
