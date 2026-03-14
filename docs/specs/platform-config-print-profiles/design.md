# Design: Platform-Specific Config with Print Profiles

**Date:** 2026-03-14
**Status:** Approved

## Problem

Publishers need per-platform file attachments (print profiles — typically `.3mf` files) alongside the existing common fields. The current manifest `Platforms` dictionary stores raw `JsonElement` values, requiring manual property extraction. There is no shared model for platform config, and no canonical place to put shared optional fields like print profiles.

## Goal

- Add `print_profiles` as a list of relative file paths on each platform's config block.
- Introduce a typed `PlatformConfig` base record so publishers get IDE-supported, type-safe access to platform config.
- Keep the JSON manifest format backward-compatible (existing manifests without `print_profiles` continue to work).
- Make it easy to add future platform-specific fields without touching shared code.

## Non-Goals

- Uploading or validating print profile files (responsibility of each publisher).
- Changing the platform key format or the `Platforms` dictionary structure.

---

## Architecture

### New: `Models/PlatformConfig.cs`

Shared base record for all platform config blocks:

```csharp
public record PlatformConfig
{
    [JsonPropertyName("tier")]
    public string Tier { get; init; } = "free";

    [JsonPropertyName("print_profiles")]
    public List<string> PrintProfiles { get; init; } = [];
}
```

`PrintProfiles` contains relative paths resolved via the existing `manifest.ResolveFilePath()`.

### New: `Platforms/PatreonConfig.cs`

Moves Patreon's existing extra fields from ad-hoc `JsonElement` access into a typed subclass:

```csharp
public record PatreonConfig : PlatformConfig
{
    [JsonPropertyName("free_post")]
    public bool FreePost { get; init; } = true;

    [JsonPropertyName("access_tier_id")]
    public string? AccessTierId { get; init; }
}
```

### Updated: `Models/ReleaseManifest.cs`

`Platforms` stays `Dictionary<string, JsonElement>` — no change to deserialization or JSON format.

Add one helper method:

```csharp
public T? GetPlatformConfig<T>(string platformKey) where T : PlatformConfig, new()
{
    if (!Platforms.TryGetValue(platformKey, out var el))
        return null;
    return JsonSerializer.Deserialize<T>(el, JsonOptions) ?? new T();
}
```

Returns `null` when the platform key is absent — preserving the existing gate in `PublishCommand` where `.Where(x => x.Tier != null)` filters out unlisted platforms. A private static `JsonOptions` with `PropertyNameCaseInsensitive = true` is added to the class (deserialization only; `WriteIndented` is irrelevant here). No custom converter is needed: `JsonSerializer.Deserialize<PatreonConfig>(el, options)` works correctly for concrete derived types without any `[JsonDerivedType]` attributes.

### Updated: `PublishCommand.cs`

`ResolveTier` replaces its manual `JsonElement` property extraction with:

```csharp
var config = manifest.GetPlatformConfig<PlatformConfig>(publisher.PlatformKey);
if (config is null) return null;
return config.Tier is "free" or "premium" ? config.Tier : "free";
```

The `null` return when the key is absent is preserved — `PublishCommand`'s existing `.Where(x => x.Tier != null)` gate continues to filter out platforms not listed in the manifest.

### Updated: `Platforms/PatreonPublisher.cs`

`PatreonPublisher` currently has no `JsonElement` access — its extra fields (`free_post`, `access_tier_id`) are not yet read from the manifest. This change adds first-time typed access: the publisher calls `manifest.GetPlatformConfig<PatreonConfig>(PlatformKey)` so that when those fields are implemented they use the typed config rather than raw `JsonElement`.

---

## Manifest JSON Format

No breaking changes. `print_profiles` is optional and defaults to an empty list when omitted. Platforms that do not use print profiles simply leave the key out — `PlatformConfig.PrintProfiles` will be `[]`.

```json
"platforms": {
  "printables": {
    "tier": "free",
    "print_profiles": ["./profiles/printables-0.2mm.3mf"]
  },
  "makerworld": {
    "tier": "free",
    "print_profiles": [
      "./profiles/makerworld-0.2mm.3mf",
      "./profiles/makerworld-0.4mm.3mf"
    ]
  },
  "patreon": {
    "tier": "premium",
    "free_post": false,
    "access_tier_id": "YOUR_TIER_ID_HERE"
    // print_profiles omitted — defaults to []
  }
}
```

Note: path resolution for print profiles uses the existing `manifest.ResolveFilePath()`. File existence is not validated at load time — that is the responsibility of each publisher at upload time.

---

## Data Flow

1. `PublishCommand` calls `manifest.GetPlatformConfig<PlatformConfig>(publisher.PlatformKey)` to resolve tier.
2. Publisher receives `manifest` as before.
3. Publisher calls `manifest.GetPlatformConfig<T>(PlatformKey)` to get its typed config.
4. Publisher iterates `config.PrintProfiles`, calls `manifest.ResolveFilePath(path)` on each to get absolute paths.
5. Publisher uploads resolved paths using the existing `FileUploadHelper`.

---

## Adding Future Platform-Specific Fields

- If a platform needs unique fields: create `MyPlatformConfig : PlatformConfig` and call `GetPlatformConfig<MyPlatformConfig>()`.
- If a field is useful across all platforms: add it to `PlatformConfig` directly.
- No changes required to `ReleaseManifest`, `PublishCommand`, or any other publisher.

---

## Files Changed

| File | Change |
|------|--------|
| `src/ModelPublisher.Core/Models/PlatformConfig.cs` | **New** — base record |
| `src/ModelPublisher.Core/Platforms/PatreonConfig.cs` | **New** — Patreon-specific subclass |
| `src/ModelPublisher.Core/Models/ReleaseManifest.cs` | Add `GetPlatformConfig<T>()` helper |
| `src/ModelPublisher.Core/PublishCommand.cs` | Simplify `ResolveTier` |
| `src/ModelPublisher.Core/Platforms/PatreonPublisher.cs` | Use `PatreonConfig` instead of raw `JsonElement` |
| `releases/example-model/manifest.json` | Add `print_profiles` example entries |
