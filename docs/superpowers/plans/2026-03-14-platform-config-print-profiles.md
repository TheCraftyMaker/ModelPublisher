# Platform-Specific Config & Print Profiles Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Introduce a typed `PlatformConfig` base record with `print_profiles` support, a `PatreonConfig` subclass, a `GetPlatformConfig<T>()` helper on `ReleaseManifest`, and simplify `ResolveTier` in `PublishCommand`.

**Architecture:** A new `PlatformConfig` record lives in `Models/` and holds the fields common to all platform config blocks (`tier`, `print_profiles`). Platform-specific subclasses (starting with `PatreonConfig`) extend it. `ReleaseManifest` keeps its `Dictionary<string, JsonElement>` storage but gains a generic `GetPlatformConfig<T>()` method that deserializes on demand.

**Tech Stack:** .NET 10, C#, System.Text.Json, xUnit 2.x, FluentAssertions

---

## Chunk 1: Test project + PlatformConfig base + GetPlatformConfig<T>

### Task 1: Create the test project

No test project currently exists. We create one and wire it into the solution.

**Files:**
- Create: `tests/ModelPublisher.Core.Tests/ModelPublisher.Core.Tests.csproj`
- Modify: `ModelPublisher.sln`

- [ ] **Step 1: Scaffold test project**

Run from the **worktree root** (`C:\Source\ModelPublisher\.claude\worktrees\flamboyant-spence`):

```bash
cd /c/Source/ModelPublisher/.claude/worktrees/flamboyant-spence
dotnet new xunit -n ModelPublisher.Core.Tests -o tests/ModelPublisher.Core.Tests --framework net10.0
```

Expected: `tests/ModelPublisher.Core.Tests/` created with a `.csproj` and `UnitTest1.cs`.

- [ ] **Step 2: Add FluentAssertions and reference Core**

Edit `tests/ModelPublisher.Core.Tests/ModelPublisher.Core.Tests.csproj` to match:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" Version="6.12.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\ModelPublisher.Core\ModelPublisher.Core.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Add project to solution**

```bash
cd /c/Source/ModelPublisher/.claude/worktrees/flamboyant-spence
dotnet sln add tests/ModelPublisher.Core.Tests/ModelPublisher.Core.Tests.csproj
```

- [ ] **Step 4: Delete the placeholder test file**

```bash
rm tests/ModelPublisher.Core.Tests/UnitTest1.cs
```

- [ ] **Step 5: Verify build**

```bash
dotnet build
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 6: Commit**

```bash
git add tests/ ModelPublisher.sln
git commit -m "$(cat <<'EOF'
Add ModelPublisher.Core.Tests xUnit project

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: PlatformConfig base record

**Files:**
- Create: `src/ModelPublisher.Core/Models/PlatformConfig.cs`
- Create: `tests/ModelPublisher.Core.Tests/Models/PlatformConfigTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/ModelPublisher.Core.Tests/Models/PlatformConfigTests.cs`:

```csharp
using System.Text.Json;
using FluentAssertions;
using ModelPublisher.Core.Models;

namespace ModelPublisher.Core.Tests.Models;

public class PlatformConfigTests
{
    [Fact]
    public void Deserialize_WithTierAndProfiles_PopulatesBothFields()
    {
        var json = """{"tier":"premium","print_profiles":["./a.3mf","./b.3mf"]}""";
        var config = JsonSerializer.Deserialize<PlatformConfig>(json);
        config!.Tier.Should().Be("premium");
        config.PrintProfiles.Should().Equal("./a.3mf", "./b.3mf");
    }

    [Fact]
    public void Deserialize_WithNoFields_UsesDefaults()
    {
        var json = "{}";
        var config = JsonSerializer.Deserialize<PlatformConfig>(json);
        config!.Tier.Should().Be("free");
        config.PrintProfiles.Should().BeEmpty();
    }

    [Fact]
    public void Deserialize_WithNoPrintProfiles_DefaultsToEmpty()
    {
        var json = """{"tier":"free"}""";
        var config = JsonSerializer.Deserialize<PlatformConfig>(json);
        config!.PrintProfiles.Should().BeEmpty();
    }
}
```

- [ ] **Step 2: Run tests — expect compile failure**

```bash
cd /c/Source/ModelPublisher/.claude/worktrees/flamboyant-spence
dotnet test tests/ModelPublisher.Core.Tests/
```

Expected: Build error — `PlatformConfig` does not exist yet.

- [ ] **Step 3: Create PlatformConfig**

Create `src/ModelPublisher.Core/Models/PlatformConfig.cs`:

```csharp
using System.Text.Json.Serialization;

namespace ModelPublisher.Core.Models;

public record PlatformConfig
{
    [JsonPropertyName("tier")]
    public string Tier { get; init; } = "free";

    [JsonPropertyName("print_profiles")]
    public List<string> PrintProfiles { get; init; } = [];
}
```

- [ ] **Step 4: Run tests — expect pass**

```bash
dotnet test tests/ModelPublisher.Core.Tests/ --logger "console;verbosity=normal"
```

Expected: 3 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/ModelPublisher.Core/Models/PlatformConfig.cs \
        tests/ModelPublisher.Core.Tests/Models/PlatformConfigTests.cs
git commit -m "$(cat <<'EOF'
Add PlatformConfig base record with tier and print_profiles

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
EOF
)"
```

---

### Task 3: PatreonConfig subclass

**Files:**
- Create: `src/ModelPublisher.Core/Platforms/PatreonConfig.cs`
- Create: `tests/ModelPublisher.Core.Tests/Platforms/PatreonConfigTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/ModelPublisher.Core.Tests/Platforms/PatreonConfigTests.cs`:

```csharp
using System.Text.Json;
using FluentAssertions;
using ModelPublisher.Core.Platforms;

namespace ModelPublisher.Core.Tests.Platforms;

public class PatreonConfigTests
{
    [Fact]
    public void Deserialize_WithAllFields_PopulatesCorrectly()
    {
        var json = """{"tier":"premium","free_post":false,"access_tier_id":"tier_abc123"}""";
        var config = JsonSerializer.Deserialize<PatreonConfig>(json);
        config!.Tier.Should().Be("premium");
        config.FreePost.Should().BeFalse();
        config.AccessTierId.Should().Be("tier_abc123");
        config.PrintProfiles.Should().BeEmpty();
    }

    [Fact]
    public void Deserialize_WithNoOptionalFields_UsesDefaults()
    {
        var json = "{}";
        var config = JsonSerializer.Deserialize<PatreonConfig>(json);
        config!.Tier.Should().Be("free");
        config.FreePost.Should().BeTrue();
        config.AccessTierId.Should().BeNull();
    }

    [Fact]
    public void Deserialize_InheritsBasePrintProfiles()
    {
        var json = """{"print_profiles":["./x.3mf"]}""";
        var config = JsonSerializer.Deserialize<PatreonConfig>(json);
        config!.PrintProfiles.Should().Equal("./x.3mf");
    }
}
```

- [ ] **Step 2: Run tests — expect compile failure**

```bash
dotnet test tests/ModelPublisher.Core.Tests/
```

Expected: Build error — `PatreonConfig` does not exist.

- [ ] **Step 3: Create PatreonConfig**

Create `src/ModelPublisher.Core/Platforms/PatreonConfig.cs`:

```csharp
using System.Text.Json.Serialization;
using ModelPublisher.Core.Models;

namespace ModelPublisher.Core.Platforms;

public record PatreonConfig : PlatformConfig
{
    [JsonPropertyName("free_post")]
    public bool FreePost { get; init; } = true;

    [JsonPropertyName("access_tier_id")]
    public string? AccessTierId { get; init; }
}
```

- [ ] **Step 4: Run tests — expect pass**

```bash
dotnet test tests/ModelPublisher.Core.Tests/ --logger "console;verbosity=normal"
```

Expected: All tests pass (including previous 3).

- [ ] **Step 5: Commit**

```bash
git add src/ModelPublisher.Core/Platforms/PatreonConfig.cs \
        tests/ModelPublisher.Core.Tests/Platforms/PatreonConfigTests.cs
git commit -m "$(cat <<'EOF'
Add PatreonConfig subclass with free_post and access_tier_id

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
EOF
)"
```

---

### Task 4: GetPlatformConfig<T>() on ReleaseManifest

**Files:**
- Modify: `src/ModelPublisher.Core/Models/ReleaseManifest.cs`
- Create: `tests/ModelPublisher.Core.Tests/Models/ReleaseManifestGetPlatformConfigTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/ModelPublisher.Core.Tests/Models/ReleaseManifestGetPlatformConfigTests.cs`:

```csharp
using System.Text.Json;
using FluentAssertions;
using ModelPublisher.Core.Models;
using ModelPublisher.Core.Platforms;

namespace ModelPublisher.Core.Tests.Models;

public class ReleaseManifestGetPlatformConfigTests
{
    private static ReleaseManifest DeserializeManifest(string platformsJson)
    {
        var json = $$"""
            {
              "title": "Test",
              "platforms": {{platformsJson}}
            }
            """;
        return JsonSerializer.Deserialize<ReleaseManifest>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    [Fact]
    public void GetPlatformConfig_AbsentKey_ReturnsNull()
    {
        var manifest = DeserializeManifest("""{"printables":{"tier":"free"}}""");
        manifest.GetPlatformConfig<PlatformConfig>("makerworld").Should().BeNull();
    }

    [Fact]
    public void GetPlatformConfig_BaseType_DeserializesTierAndProfiles()
    {
        var manifest = DeserializeManifest(
            """{"printables":{"tier":"premium","print_profiles":["./profile.3mf"]}}""");
        var config = manifest.GetPlatformConfig<PlatformConfig>("printables");
        config!.Tier.Should().Be("premium");
        config.PrintProfiles.Should().Equal("./profile.3mf");
    }

    [Fact]
    public void GetPlatformConfig_DerivedType_DeserializesExtraFields()
    {
        var manifest = DeserializeManifest(
            """{"patreon":{"tier":"premium","free_post":false,"access_tier_id":"t123"}}""");
        var config = manifest.GetPlatformConfig<PatreonConfig>("patreon");
        config!.Tier.Should().Be("premium");
        config.FreePost.Should().BeFalse();
        config.AccessTierId.Should().Be("t123");
    }

    [Fact]
    public void GetPlatformConfig_NoOptionalFields_ReturnsDefaults()
    {
        var manifest = DeserializeManifest("""{"printables":{}}""");
        var config = manifest.GetPlatformConfig<PlatformConfig>("printables");
        config!.Tier.Should().Be("free");
        config.PrintProfiles.Should().BeEmpty();
    }
}
```

- [ ] **Step 2: Run tests — expect compile failure**

```bash
dotnet test tests/ModelPublisher.Core.Tests/
```

Expected: Build error — `GetPlatformConfig` does not exist.

- [ ] **Step 3: Add GetPlatformConfig<T>() and JsonOptions to ReleaseManifest**

Open `src/ModelPublisher.Core/Models/ReleaseManifest.cs`. Add a `using System.Text.Json;` at the top (it's already there). Then add to the `ReleaseManifest` class body, after the existing `ResolveFilePath` method:

```csharp
private static readonly JsonSerializerOptions ConfigJsonOptions = new()
{
    PropertyNameCaseInsensitive = true
};

public T? GetPlatformConfig<T>(string platformKey) where T : PlatformConfig, new()
{
    if (!Platforms.TryGetValue(platformKey, out var el))
        return null;
    return JsonSerializer.Deserialize<T>(el, ConfigJsonOptions) ?? new T();
}
```

- [ ] **Step 4: Run tests — expect pass**

```bash
dotnet test tests/ModelPublisher.Core.Tests/ --logger "console;verbosity=normal"
```

Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/ModelPublisher.Core/Models/ReleaseManifest.cs \
        tests/ModelPublisher.Core.Tests/Models/ReleaseManifestGetPlatformConfigTests.cs
git commit -m "$(cat <<'EOF'
Add GetPlatformConfig<T>() helper to ReleaseManifest

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
EOF
)"
```

---

## Chunk 2: Wire up PublishCommand + PatreonPublisher + example manifest

### Task 5: Simplify ResolveTier in PublishCommand

**Files:**
- Modify: `src/ModelPublisher.Core/PublishCommand.cs`

No new tests needed — `ResolveTier` is a private static method tested via integration (running the CLI). The behavior is unchanged; this is purely a simplification refactor.

- [ ] **Step 1: Update ResolveTier**

In `src/ModelPublisher.Core/PublishCommand.cs`, replace the `ResolveTier` method (lines 147–160):

```csharp
private static string? ResolveTier(ReleaseManifest manifest, IPlatformPublisher publisher)
{
    var config = manifest.GetPlatformConfig<PlatformConfig>(publisher.PlatformKey);
    if (config is null) return null;
    var tier = config.Tier.ToLowerInvariant();
    return tier is "free" or "premium" ? tier : "free";
}
```

- [ ] **Step 2: Build to verify no compile errors**

```bash
dotnet build
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Run all tests**

```bash
dotnet test tests/ModelPublisher.Core.Tests/ --logger "console;verbosity=normal"
```

Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/ModelPublisher.Core/PublishCommand.cs
git commit -m "$(cat <<'EOF'
Simplify ResolveTier to use GetPlatformConfig<T>

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
EOF
)"
```

---

### Task 6: Update PatreonPublisher to use PatreonConfig

**Files:**
- Modify: `src/ModelPublisher.Core/Platforms/PatreonPublisher.cs`

`PatreonPublisher` currently reads neither `free_post` nor `access_tier_id` from the manifest. This task adds the call to `GetPlatformConfig<PatreonConfig>()` so that when those fields are implemented they use the typed config. No behavior changes are introduced.

- [ ] **Step 1: Add PatreonConfig access in PublishFreeAsync**

In `src/ModelPublisher.Core/Platforms/PatreonPublisher.cs`, inside `PublishFreeAsync`, add the config retrieval immediately after the `try {` opening (before the `GotoAsync` call):

```csharp
// TODO: use config.FreePost and config.AccessTierId when Patreon automation is implemented
var config = manifest.GetPlatformConfig<PatreonConfig>(PlatformKey) ?? new PatreonConfig();
_ = config;
```

The `?? new PatreonConfig()` fallback is safe — Patreon is only run when the key is present in the manifest, but this keeps the code null-safe. The `_ = config` discard suppresses the unused-variable warning since the fields are not yet consumed.

- [ ] **Step 2: Build and run all tests**

```bash
dotnet build && dotnet test tests/ModelPublisher.Core.Tests/ --logger "console;verbosity=normal"
```

Expected: Build succeeded, all tests pass.

- [ ] **Step 3: Commit**

```bash
git add src/ModelPublisher.Core/Platforms/PatreonPublisher.cs
git commit -m "$(cat <<'EOF'
Use PatreonConfig in PatreonPublisher for typed config access

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
EOF
)"
```

---

### Task 7: Update example manifest with print_profiles

**Files:**
- Modify: `releases/example-model/manifest.json`

- [ ] **Step 1: Add print_profiles to two platforms in the example manifest**

Update `releases/example-model/manifest.json` — add `print_profiles` to `printables` and `makerworld` to demonstrate the feature. Keep other platforms unchanged.

```json
{
  "title": "Modular Cable Management Clips",
  "description": "...",
  "tags": ["cable-management", "desk", "organizer", "snap-fit", "modular"],
  "license": "CC-BY-4.0",
  "files": {
    "models": [
      "./cable-clip-narrow.3mf",
      "./cable-clip-wide.3mf"
    ],
    "cover": "./photo1.jpg",
    "photos": [
      "./photo-mounted.jpg",
      "./photo-detail.jpg"
    ]
  },
  "platforms": {
    "printables": {
      "tier": "free",
      "print_profiles": ["./profiles/printables-0.2mm-pla.3mf"]
    },
    "makerworld": {
      "tier": "free",
      "print_profiles": ["./profiles/makerworld-0.2mm-pla.3mf"]
    },
    "cults3d": {
      "tier": "free"
    },
    "thangs": {
      "tier": "free"
    },
    "makeronline": {
      "tier": "free"
    },
    "patreon": {
      "tier": "premium",
      "free_post": false,
      "access_tier_id": "YOUR_TIER_ID_HERE"
    }
  }
}
```

Note: The `./profiles/` files don't need to physically exist in the example — they're illustrative. Publishers only resolve paths when uploading.

- [ ] **Step 2: Build to ensure manifest change doesn't break anything**

```bash
dotnet build
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add releases/example-model/manifest.json
git commit -m "$(cat <<'EOF'
Add print_profiles examples to example manifest

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
EOF
)"
```

---

### Task 8: Push and open PR

- [ ] **Step 1: Push branch**

```bash
git push
```

- [ ] **Step 2: Open PR**

```powershell
powershell.exe -Command "& 'C:\Program Files\GitHub CLI\gh.exe' pr create --repo TheCraftyMaker/ModelPublisher --title 'Add typed platform config with print_profiles support' --base master --head claude/flamboyant-spence --body 'Introduces a typed PlatformConfig base record and GetPlatformConfig<T>() helper on ReleaseManifest, replacing manual JsonElement parsing. Adds print_profiles as a list of relative file paths per platform. PatreonConfig subclass captures Patreon-specific fields. ResolveTier in PublishCommand simplified. Backward-compatible — existing manifests without print_profiles continue to work.'"
```

- [ ] **Step 3: Share PR URL with user**
