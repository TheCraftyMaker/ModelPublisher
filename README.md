[![.NET](https://github.com/TheCraftyMaker/ModelPublisher/actions/workflows/dotnet.yml/badge.svg)](https://github.com/TheCraftyMaker/ModelPublisher/actions/workflows/dotnet.yml) [![CodeQL Advanced](https://github.com/TheCraftyMaker/ModelPublisher/actions/workflows/codeql.yml/badge.svg)](https://github.com/TheCraftyMaker/ModelPublisher/actions/workflows/codeql.yml) [![Dependabot Updates](https://github.com/TheCraftyMaker/ModelPublisher/actions/workflows/dependabot/dependabot-updates/badge.svg)](https://github.com/TheCraftyMaker/ModelPublisher/actions/workflows/dependabot/dependabot-updates)

# Automated 3D Model Publisher

Automates publishing 3D printable models across multiple platforms using Playwright browser automation.

## Platforms

| Key | Platform | Tiers | Status |
|---|---|---|---|
| `printables` | Printables.com | Free + Premium | Working |
| `makerworld` | MakerWorld.com | Free | Stub — needs codegen selectors |
| `cults3d` | Cults3D.com | Free | Stub — needs codegen selectors |
| `thangs` | Thangs.com | Free + Premium | Working |
| `makeronline` | Maker.online | Free | Stub — needs codegen selectors |
| `patreon` | Patreon.com | Free + Premium | Stub — clipboard paste approach, needs verification |

## Prerequisites

- .NET 10 SDK
- [Brave Browser](https://brave.com/) installed at the default path (`C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe`)
- Run once after build to install Playwright browsers:

```bash
pwsh src/ModelPublisher.Cli/bin/Debug/net10.0/playwright.ps1 install chromium
```

## Setup

1. Clone the repo
2. Build the solution:
   ```bash
   dotnet build
   ```
3. Install Playwright's Chromium browser (one-time):
   ```bash
   pwsh src/ModelPublisher.Cli/bin/Debug/net10.0/playwright.ps1 install chromium
   ```
4. On first run per platform, log in manually when prompted. Sessions are persisted in `profiles/` so you won't need to log in again until cookies expire.

## Implementing a stub platform

For platforms marked as stubs, use Playwright's codegen tool to capture live selectors, then fill them in to the publisher class:

```bash
# Close Brave first, then run:
dotnet run --project src\ModelPublisher.Cli -- codegen <platformKey> <url> --profile-path "C:\Users\chris\AppData\Local\BraveSoftware\Brave-Browser\User Data\Profile 1"
```

The `--profile-path` option lets you reuse your real Brave session (already logged in). Brave must be fully closed before running this command.

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

# Use your real Brave profile to bypass Cloudflare (close Brave first)
dotnet run --project src/ModelPublisher.Cli -- releases/my-model-name/manifest.json --platforms makerworld --profile-path "C:\Users\chris\AppData\Local\BraveSoftware\Brave-Browser\User Data\Profile 1"
```

## Auth

On first run per platform, if you're not logged in, the browser will pause and prompt you to log in manually. After logging in, press Enter in the terminal. The session is saved to `profiles/<platform>/` and reused on subsequent runs.

The `profiles/` directory is gitignored. Do not commit it — it contains your session cookies.

### Cloudflare-protected platforms (MakerWorld, Patreon)

Cloudflare detects Playwright's CDP protocol at the network level. Use `--stealth` to launch via [playwright-extra](https://github.com/berstend/puppeteer-extra) with the stealth plugin, which patches the browser to remove automation markers:

```bash
# Stealth mode (bypasses Cloudflare bot detection)
dotnet run --project src\ModelPublisher.Cli -- "manifest.json" --platforms makerworld --stealth

# Stealth + real Brave profile (best combination — close Brave first)
dotnet run --project src\ModelPublisher.Cli -- "manifest.json" --platforms makerworld --stealth --profile-path "C:\Users\chris\AppData\Local\BraveSoftware\Brave-Browser\User Data\Profile 1"

# Stealth codegen (for inspecting selectors on Cloudflare-protected sites)
dotnet run --project src\ModelPublisher.Cli -- codegen makerworld https://makerworld.com/en/my/models/publish?type=original --stealth
```

Requires Node.js and `npm install` in the project root (one-time setup).

## Adding a new platform

1. Create `src/ModelPublisher.Core/Platforms/MyNewPublisher.cs` implementing `IPlatformPublisher`.
2. Add it to the `_publishers` list in `PublishCommand.cs`.
3. Add the platform key to `manifest.json`.
