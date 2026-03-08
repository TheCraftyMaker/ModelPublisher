# ModelPublisher

Automates publishing 3D printable models across multiple platforms using Playwright browser automation.

## Platforms

| Key | Platform | Notes |
|---|---|---|
| `printables` | Printables.com | Free + Premium |
| `makerworld` | MakerWorld.com | Free |
| `cults3d` | Cults3D.com | Free |
| `thangs` | Thangs.com | Free + Premium |
| `makeronline` | Maker.online | Free |
| `patreon` | Patreon.com | Free + Premium — semi-automated, always requires human review |

## Prerequisites

- .NET 9 SDK
- Run once after build to install Playwright browsers:

```bash
pwsh bin/Debug/net9.0/playwright.ps1 install chromium
```

Or on Windows without PowerShell Core:

```bash
dotnet run --project src/ModelPublisher.Cli -- --help
# then:
.\bin\Debug\net9.0\playwright.ps1 install chromium
```

## Setup

1. Clone or copy to `C:\Source\ModelPublisher`
2. Build the solution:
   ```bash
   dotnet build
   ```
3. Install Playwright's Chromium browser (one-time):
   ```bash
   dotnet run --project src/ModelPublisher.Cli -- --help
   pwsh src/ModelPublisher.Cli/bin/Debug/net9.0/playwright.ps1 install chromium
   ```
4. On first run per platform, log in manually when prompted. Sessions are persisted in `profiles/` so you won't need to log in again until cookies expire.

## Selectors

The platform scripts contain placeholder selectors that **will need to be updated** before first use. Use Playwright's codegen tool to capture live selectors:

```bash
pwsh src/ModelPublisher.Cli/bin/Debug/net9.0/playwright.ps1 codegen --target csharp https://www.printables.com/model/create
```

Replace the `TODO` selectors in each publisher class with what codegen captures.

## Release Workflow

1. Design, print, and photograph your model.
2. Write your title and description (with LLM assistance as desired).
3. Create a release folder:
   ```
   releases/
     my-model-name/
       manifest.json
       my-model.3mf
       photo1.jpg
       photo2.jpg
   ```
4. Fill in `manifest.json` (copy from `releases/example-model/manifest.json`).
5. Publish:

```bash
# Publish to all platforms in manifest
dotnet run --project src/ModelPublisher.Cli -- releases/my-model-name/manifest.json

# Publish to specific platforms only
dotnet run --project src/ModelPublisher.Cli -- releases/my-model-name/manifest.json --platforms printables makerworld

# From C:\Source\ModelPublisher:
dotnet run --project src/ModelPublisher.Cli -- "C:\Source\ModelPublisher\releases\my-model-name\manifest.json"
```

## Human-in-the-loop steps

Every platform pauses before the final publish action and waits for you to press Enter. This lets you:
- Catch any mis-filled fields
- Select categories or subcategories not covered by automation
- Handle CAPTCHAs if they appear

Patreon additionally requires manual access tier selection due to the complexity of their UI.

## Auth

On first run per platform, if you're not logged in, the browser will pause and prompt you to log in manually. After logging in, press Enter in the terminal. The session is saved to `profiles/<platform>/` and reused on subsequent runs.

The `profiles/` directory is gitignored. Do not commit it — it contains your session cookies.

## Adding a new platform

1. Create `src/ModelPublisher.Core/Platforms/MyNewPublisher.cs` implementing `IPlatformPublisher`.
2. Add it to the `_publishers` list in `PublishCommand.cs`.
3. Add the platform key to `manifest.json`.
