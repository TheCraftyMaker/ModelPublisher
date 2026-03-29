# Manifest Editor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a cross-platform Avalonia desktop GUI for creating and editing ModelPublisher `manifest.json` files.

**Architecture:** Avalonia 11 + CommunityToolkit.Mvvm; sidebar-nav shell with four content sections (Basic Info, Files, Description, Platforms); a pure-C# `ManifestEditorState` converts to/from `ReleaseManifest`; ViewModels hold editing state; Views are AXAML-only.

**Tech Stack:** .NET 10, Avalonia 11.2.3, CommunityToolkit.Mvvm 8.4.0, xUnit 2.9.3, FluentAssertions 6.12.2

---

## File Map

| Path | Purpose |
|------|---------|
| `src/ModelPublisher.ManifestEditor/ModelPublisher.ManifestEditor.csproj` | Project file |
| `src/ModelPublisher.ManifestEditor/Program.cs` | Entry point |
| `src/ModelPublisher.ManifestEditor/App.axaml` + `.cs` | Avalonia application bootstrap |
| `src/ModelPublisher.ManifestEditor/Models/ManifestEditorState.cs` | Serialization model: `FromManifest()` / `ToJson()` |
| `src/ModelPublisher.ManifestEditor/Models/PlatformState.cs` | Per-platform editing state |
| `src/ModelPublisher.ManifestEditor/Models/ManifestValidator.cs` | Pre-save validation rules |
| `src/ModelPublisher.ManifestEditor/Services/IFileDialogService.cs` | Interface for file/folder picker + dialogs |
| `src/ModelPublisher.ManifestEditor/Services/AvaloniaFileDialogService.cs` | Avalonia StorageProvider implementation |
| `src/ModelPublisher.ManifestEditor/ViewModels/FileEntryViewModel.cs` | Single file path entry (path, isMissing) |
| `src/ModelPublisher.ManifestEditor/ViewModels/FilesViewModel.cs` | Models + photos lists with add/remove/reorder |
| `src/ModelPublisher.ManifestEditor/ViewModels/BasicInfoViewModel.cs` | Title, tags, license |
| `src/ModelPublisher.ManifestEditor/ViewModels/DescriptionViewModel.cs` | Markdown text |
| `src/ModelPublisher.ManifestEditor/ViewModels/PlatformEntryViewModel.cs` | IsEnabled + tier + print profiles + Patreon fields |
| `src/ModelPublisher.ManifestEditor/ViewModels/PlatformsViewModel.cs` | Collection of PlatformEntryViewModel |
| `src/ModelPublisher.ManifestEditor/ViewModels/MainWindowViewModel.cs` | Section switching, dirty tracking, open/save commands |
| `src/ModelPublisher.ManifestEditor/Views/MainWindow.axaml` + `.cs` | Shell: sidebar + ContentControl |
| `src/ModelPublisher.ManifestEditor/Views/BasicInfoView.axaml` + `.cs` | Title textbox, tag chips, license dropdown |
| `src/ModelPublisher.ManifestEditor/Views/FilesView.axaml` + `.cs` | File lists with up/down/remove buttons |
| `src/ModelPublisher.ManifestEditor/Views/DescriptionView.axaml` + `.cs` | Monospace text editor |
| `src/ModelPublisher.ManifestEditor/Views/PlatformsView.axaml` + `.cs` | Toggle + expand per platform |
| `tests/ModelPublisher.ManifestEditor.Tests/ModelPublisher.ManifestEditor.Tests.csproj` | Test project |
| `tests/ModelPublisher.ManifestEditor.Tests/ManifestEditorStateTests.cs` | ToJson / FromManifest round-trip |
| `tests/ModelPublisher.ManifestEditor.Tests/ManifestValidatorTests.cs` | All validation rules |
| `tests/ModelPublisher.ManifestEditor.Tests/FilesViewModelTests.cs` | Add/remove/reorder logic |
| `tests/ModelPublisher.ManifestEditor.Tests/BasicInfoViewModelTests.cs` | Tag management |
| `tests/ModelPublisher.ManifestEditor.Tests/PlatformsViewModelTests.cs` | Toggle and Patreon fields |

---

## Task 1: Branch + Project Scaffold

**Files:**
- Create: `src/ModelPublisher.ManifestEditor/ModelPublisher.ManifestEditor.csproj`
- Create: `src/ModelPublisher.ManifestEditor/Program.cs`
- Create: `src/ModelPublisher.ManifestEditor/App.axaml`
- Create: `src/ModelPublisher.ManifestEditor/App.axaml.cs`
- Create: `src/ModelPublisher.ManifestEditor/Views/MainWindow.axaml`
- Create: `src/ModelPublisher.ManifestEditor/Views/MainWindow.axaml.cs`
- Create: `src/ModelPublisher.ManifestEditor/Services/IFileDialogService.cs`
- Create: `tests/ModelPublisher.ManifestEditor.Tests/ModelPublisher.ManifestEditor.Tests.csproj`
- Modify: `ModelPublisher.sln`

- [ ] **Step 1: Create the feature branch**

```bash
cd C:/Source/ModelPublisher
git checkout -b feat/manifest-editor
```

- [ ] **Step 2: Create directory structure**

```bash
mkdir -p src/ModelPublisher.ManifestEditor/{Models,Services,ViewModels,Views}
mkdir -p tests/ModelPublisher.ManifestEditor.Tests
```

- [ ] **Step 3: Create the csproj**

```xml
<!-- src/ModelPublisher.ManifestEditor/ModelPublisher.ManifestEditor.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <BuiltInComInteropSupport>true</BuiltInComInteropSupport>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.2.3" />
    <PackageReference Include="Avalonia.Desktop" Version="11.2.3" />
    <PackageReference Include="Avalonia.Themes.Fluent" Version="11.2.3" />
    <PackageReference Include="Avalonia.Fonts.Inter" Version="11.2.3" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ModelPublisher.Core\ModelPublisher.Core.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Create Program.cs**

```csharp
// src/ModelPublisher.ManifestEditor/Program.cs
using Avalonia;
using ModelPublisher.ManifestEditor;

return AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .WithInterFont()
    .LogToTrace()
    .StartWithClassicDesktopLifetime(args);
```

- [ ] **Step 5: Create App.axaml**

```xml
<!-- src/ModelPublisher.ManifestEditor/App.axaml -->
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="ModelPublisher.ManifestEditor.App"
             RequestedThemeVariant="Dark">
    <Application.Styles>
        <FluentTheme />
    </Application.Styles>
</Application>
```

- [ ] **Step 6: Create App.axaml.cs**

```csharp
// src/ModelPublisher.ManifestEditor/App.axaml.cs
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ModelPublisher.ManifestEditor.Views;

namespace ModelPublisher.ManifestEditor;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
```

- [ ] **Step 7: Create MainWindow.axaml (placeholder)**

```xml
<!-- src/ModelPublisher.ManifestEditor/Views/MainWindow.axaml -->
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="ModelPublisher.ManifestEditor.Views.MainWindow"
        Title="Manifest Editor"
        Width="900" Height="650">
    <TextBlock Text="Loading..." HorizontalAlignment="Center" VerticalAlignment="Center" />
</Window>
```

- [ ] **Step 8: Create MainWindow.axaml.cs**

```csharp
// src/ModelPublisher.ManifestEditor/Views/MainWindow.axaml.cs
using Avalonia.Controls;

namespace ModelPublisher.ManifestEditor.Views;

public partial class MainWindow : Window { }
```

- [ ] **Step 9: Create IFileDialogService.cs**

```csharp
// src/ModelPublisher.ManifestEditor/Services/IFileDialogService.cs
namespace ModelPublisher.ManifestEditor.Services;

public interface IFileDialogService
{
    /// <summary>Opens a folder or manifest.json file picker. Returns the selected path or null.</summary>
    Task<string?> OpenManifestLocationAsync();

    /// <summary>Opens a file picker for model/photo/profile files. Returns selected absolute paths.</summary>
    Task<IReadOnlyList<string>> OpenFilesAsync(string title, params string[] extensions);

    Task ShowErrorAsync(string title, string message);

    /// <summary>Returns true if the user clicks "Save".</summary>
    Task<bool> ConfirmUnsavedChangesAsync();

    /// <summary>Shows validation errors. Returns true if user clicks "Save Anyway" (not present in v1 -- always returns false).</summary>
    Task ShowValidationErrorsAsync(IReadOnlyList<string> errors);
}
```

- [ ] **Step 10: Create the test project csproj**

```xml
<!-- tests/ModelPublisher.ManifestEditor.Tests/ModelPublisher.ManifestEditor.Tests.csproj -->
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
    <ProjectReference Include="..\..\src\ModelPublisher.ManifestEditor\ModelPublisher.ManifestEditor.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 11: Add both projects to the solution**

```bash
cd C:/Source/ModelPublisher
dotnet sln add src/ModelPublisher.ManifestEditor/ModelPublisher.ManifestEditor.csproj
dotnet sln add tests/ModelPublisher.ManifestEditor.Tests/ModelPublisher.ManifestEditor.Tests.csproj
```

- [ ] **Step 12: Build to confirm scaffold compiles**

```bash
dotnet build src/ModelPublisher.ManifestEditor/ModelPublisher.ManifestEditor.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 13: Commit**

```bash
git add src/ModelPublisher.ManifestEditor/ tests/ModelPublisher.ManifestEditor.Tests/ ModelPublisher.sln
git commit -m "feat: scaffold ManifestEditor Avalonia project

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 2: ManifestEditorState + PlatformState

**Files:**
- Create: `src/ModelPublisher.ManifestEditor/Models/ManifestEditorState.cs`
- Create: `src/ModelPublisher.ManifestEditor/Models/PlatformState.cs`
- Create: `tests/ModelPublisher.ManifestEditor.Tests/ManifestEditorStateTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/ModelPublisher.ManifestEditor.Tests/ManifestEditorStateTests.cs
using System.Text.Json;
using FluentAssertions;
using ModelPublisher.Core.Models;
using ModelPublisher.ManifestEditor.Models;

namespace ModelPublisher.ManifestEditor.Tests;

public class ManifestEditorStateTests
{
    private static ReleaseManifest MakeManifest(string dir)
    {
        var json = $$"""
        {
          "title": "My Model",
          "description": "# Hello",
          "tags": ["tag1", "tag2"],
          "license": "CC-BY-SA-4.0",
          "files": {
            "models": ["./model.3mf"],
            "photos": ["./cover.jpg", "./detail.jpg"],
            "cover": "./cover.jpg"
          },
          "platforms": {
            "printables": { "tier": "premium", "print_profiles": ["./profile.3mf"] },
            "patreon": { "tier": "free", "free_post": false, "access_tier_id": "tier_abc" }
          }
        }
        """;
        var manifest = JsonSerializer.Deserialize<ReleaseManifest>(json)!;
        manifest.ManifestDirectory = dir;
        return manifest;
    }

    [Fact]
    public void FromManifest_ReadsBasicFields()
    {
        var dir = "C:/models/mymodel";
        var manifest = MakeManifest(dir);

        var state = ManifestEditorState.FromManifest(manifest);

        state.Title.Should().Be("My Model");
        state.Description.Should().Be("# Hello");
        state.Tags.Should().Equal("tag1", "tag2");
        state.License.Should().Be("CC-BY-SA-4.0");
        state.ManifestDirectory.Should().Be(dir);
    }

    [Fact]
    public void FromManifest_ResolvesFilePathsToAbsolute()
    {
        var dir = "C:/models/mymodel";
        var manifest = MakeManifest(dir);

        var state = ManifestEditorState.FromManifest(manifest);

        state.ModelFiles.Should().Equal("C:/models/mymodel/model.3mf".Replace('/', Path.DirectorySeparatorChar));
        state.Photos.Should().HaveCount(2);
        state.Cover.Should().Be("C:/models/mymodel/cover.jpg".Replace('/', Path.DirectorySeparatorChar));
    }

    [Fact]
    public void FromManifest_PopulatesAllSixPlatforms()
    {
        var manifest = MakeManifest("C:/models/x");
        var state = ManifestEditorState.FromManifest(manifest);

        state.Platforms.Should().HaveCount(6);
        state.Platforms.Select(p => p.PlatformKey).Should()
            .Equal("printables", "makerworld", "cults3d", "thangs", "makeronline", "patreon");
    }

    [Fact]
    public void FromManifest_MarksEnabledPlatformsCorrectly()
    {
        var manifest = MakeManifest("C:/models/x");
        var state = ManifestEditorState.FromManifest(manifest);

        state.Platforms.First(p => p.PlatformKey == "printables").IsEnabled.Should().BeTrue();
        state.Platforms.First(p => p.PlatformKey == "makerworld").IsEnabled.Should().BeFalse();
        state.Platforms.First(p => p.PlatformKey == "patreon").IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void FromManifest_ReadsPatreonFields()
    {
        var manifest = MakeManifest("C:/models/x");
        var state = ManifestEditorState.FromManifest(manifest);
        var patreon = state.Platforms.First(p => p.PlatformKey == "patreon");

        patreon.FreePost.Should().BeFalse();
        patreon.AccessTierId.Should().Be("tier_abc");
    }

    [Fact]
    public void ToJson_RoundTrips_BasicFields()
    {
        var dir = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
        var state = new ManifestEditorState
        {
            Title = "Round Trip",
            Description = "desc",
            Tags = ["a", "b"],
            License = "MIT",
            ManifestDirectory = dir,
        };

        var json = state.ToJson();
        var doc = JsonDocument.Parse(json).RootElement;

        doc.GetProperty("title").GetString().Should().Be("Round Trip");
        doc.GetProperty("license").GetString().Should().Be("MIT");
        doc.GetProperty("tags").EnumerateArray().Select(e => e.GetString())
            .Should().Equal("a", "b");
    }

    [Fact]
    public void ToJson_WritesRelativeFilePaths()
    {
        var dir = Path.Combine(Path.GetTempPath(), "manifesttest");
        var state = new ManifestEditorState
        {
            Title = "T",
            ManifestDirectory = dir,
            ModelFiles = [Path.Combine(dir, "model.3mf")],
            Photos = [Path.Combine(dir, "cover.jpg")],
        };

        var json = state.ToJson();
        var doc = JsonDocument.Parse(json).RootElement;
        var modelPath = doc.GetProperty("files").GetProperty("models")[0].GetString()!;

        modelPath.Should().StartWith("./");
        modelPath.Should().NotContain("\\");
    }

    [Fact]
    public void ToJson_OnlyIncludesEnabledPlatforms()
    {
        var dir = Path.GetTempPath();
        var state = new ManifestEditorState
        {
            Title = "T",
            ManifestDirectory = dir,
            Platforms =
            [
                new PlatformState { PlatformKey = "printables", IsEnabled = true, Tier = "free" },
                new PlatformState { PlatformKey = "makerworld", IsEnabled = false, Tier = "free" },
            ]
        };

        var json = state.ToJson();
        var platforms = JsonDocument.Parse(json).RootElement.GetProperty("platforms");

        platforms.TryGetProperty("printables", out _).Should().BeTrue();
        platforms.TryGetProperty("makerworld", out _).Should().BeFalse();
    }

    [Fact]
    public void ToJson_OmitsCoverWhenNull()
    {
        var dir = Path.GetTempPath();
        var state = new ManifestEditorState { Title = "T", ManifestDirectory = dir };

        var json = state.ToJson();
        var files = JsonDocument.Parse(json).RootElement.GetProperty("files");

        files.TryGetProperty("cover", out var cover).Should().BeFalse();
        // OR the cover property exists but is null -- either is acceptable per JsonIgnoreCondition.WhenWritingNull
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
cd C:/Source/ModelPublisher
dotnet test tests/ModelPublisher.ManifestEditor.Tests/ --filter "ManifestEditorStateTests"
```

Expected: compile error (types not found yet).

- [ ] **Step 3: Create PlatformState.cs**

```csharp
// src/ModelPublisher.ManifestEditor/Models/PlatformState.cs
namespace ModelPublisher.ManifestEditor.Models;

public class PlatformState
{
    public string PlatformKey { get; set; } = "";
    public bool IsEnabled { get; set; }
    public string Tier { get; set; } = "free";
    public List<string> PrintProfiles { get; set; } = [];

    // Patreon-specific
    public bool FreePost { get; set; } = true;
    public string AccessTierId { get; set; } = "";
}
```

- [ ] **Step 4: Create ManifestEditorState.cs**

```csharp
// src/ModelPublisher.ManifestEditor/Models/ManifestEditorState.cs
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelPublisher.Core.Models;
using ModelPublisher.Core.Platforms;

namespace ModelPublisher.ManifestEditor.Models;

public class ManifestEditorState
{
    public static readonly string[] AllPlatformKeys =
        ["printables", "makerworld", "cults3d", "thangs", "makeronline", "patreon"];

    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> Tags { get; set; } = [];
    public string License { get; set; } = "CC-BY-4.0";
    public string ManifestDirectory { get; set; } = "";
    public List<string> ModelFiles { get; set; } = [];
    public List<string> Photos { get; set; } = [];
    public string? Cover { get; set; }
    public List<PlatformState> Platforms { get; set; } = [];

    public static ManifestEditorState FromManifest(ReleaseManifest manifest)
    {
        var dir = manifest.ManifestDirectory ?? "";
        var state = new ManifestEditorState
        {
            Title = manifest.Title,
            Description = manifest.Description,
            Tags = manifest.Tags.ToList(),
            License = manifest.License,
            ManifestDirectory = dir,
            ModelFiles = manifest.Files.Models.Select(manifest.ResolveFilePath).ToList(),
            Photos = manifest.Files.Photos.Select(manifest.ResolveFilePath).ToList(),
            Cover = manifest.Files.Cover is not null
                ? manifest.ResolveFilePath(manifest.Files.Cover)
                : null,
        };

        foreach (var key in AllPlatformKeys)
        {
            if (key == "patreon")
            {
                var cfg = manifest.GetPlatformConfig<PatreonConfig>("patreon");
                state.Platforms.Add(new PlatformState
                {
                    PlatformKey = "patreon",
                    IsEnabled = cfg is not null,
                    Tier = cfg?.Tier ?? "free",
                    PrintProfiles = cfg?.PrintProfiles
                        .Select(manifest.ResolveFilePath).ToList() ?? [],
                    FreePost = cfg?.FreePost ?? true,
                    AccessTierId = cfg?.AccessTierId ?? "",
                });
            }
            else
            {
                var cfg = manifest.GetPlatformConfig<PlatformConfig>(key);
                state.Platforms.Add(new PlatformState
                {
                    PlatformKey = key,
                    IsEnabled = cfg is not null,
                    Tier = cfg?.Tier ?? "free",
                    PrintProfiles = cfg?.PrintProfiles
                        .Select(manifest.ResolveFilePath).ToList() ?? [],
                });
            }
        }

        return state;
    }

    public string ToJson()
    {
        var dir = ManifestDirectory;

        var platformsDict = new Dictionary<string, object>();
        foreach (var p in Platforms.Where(p => p.IsEnabled))
        {
            if (p.PlatformKey == "patreon")
            {
                var cfg = new Dictionary<string, object?>
                {
                    ["tier"] = p.Tier,
                    ["print_profiles"] = p.PrintProfiles.Select(f => ToRelative(f, dir)).ToList(),
                    ["free_post"] = p.FreePost,
                    ["access_tier_id"] = string.IsNullOrEmpty(p.AccessTierId) ? null : p.AccessTierId,
                };
                platformsDict[p.PlatformKey] = cfg;
            }
            else
            {
                var cfg = new Dictionary<string, object>
                {
                    ["tier"] = p.Tier,
                    ["print_profiles"] = p.PrintProfiles.Select(f => ToRelative(f, dir)).ToList(),
                };
                platformsDict[p.PlatformKey] = cfg;
            }
        }

        var obj = new Dictionary<string, object?>
        {
            ["title"] = Title,
            ["description"] = Description,
            ["tags"] = Tags,
            ["license"] = License,
            ["files"] = new Dictionary<string, object?>
            {
                ["models"] = ModelFiles.Select(f => ToRelative(f, dir)).ToList(),
                ["photos"] = Photos.Select(f => ToRelative(f, dir)).ToList(),
                ["cover"] = Cover is not null ? ToRelative(Cover, dir) : null,
            },
            ["platforms"] = platformsDict,
        };

        return JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });
    }

    private static string ToRelative(string absolutePath, string directory)
    {
        if (string.IsNullOrEmpty(directory)) return absolutePath;
        var rel = Path.GetRelativePath(directory, absolutePath).Replace('\\', '/');
        return rel.StartsWith('.') ? rel : "./" + rel;
    }
}
```

- [ ] **Step 5: Run tests**

```bash
dotnet test tests/ModelPublisher.ManifestEditor.Tests/ --filter "ManifestEditorStateTests"
```

Expected: all 8 tests pass. The `ToJson_OmitsCoverWhenNull` test: the `cover` key will be absent when `Cover` is null because of `DefaultIgnoreCondition.WhenWritingNull`. If the dictionary still emits a null value, update the assertion to `cover.ValueKind.Should().Be(JsonValueKind.Null)`.

- [ ] **Step 6: Commit**

```bash
git add src/ModelPublisher.ManifestEditor/Models/ tests/ModelPublisher.ManifestEditor.Tests/ManifestEditorStateTests.cs
git commit -m "feat: add ManifestEditorState with ToJson/FromManifest

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 3: ManifestValidator

**Files:**
- Create: `src/ModelPublisher.ManifestEditor/Models/ManifestValidator.cs`
- Create: `tests/ModelPublisher.ManifestEditor.Tests/ManifestValidatorTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/ModelPublisher.ManifestEditor.Tests/ManifestValidatorTests.cs
using FluentAssertions;
using ModelPublisher.ManifestEditor.Models;

namespace ModelPublisher.ManifestEditor.Tests;

public class ManifestValidatorTests
{
    private static ManifestEditorState ValidState(string? dir = null)
    {
        var d = dir ?? Path.GetTempPath();
        return new ManifestEditorState
        {
            Title = "Valid Title",
            ManifestDirectory = d,
            ModelFiles = [Path.Combine(d, "model.3mf")],
            // photos not required for basic validity
        };
    }

    [Fact]
    public void EmptyTitle_ReturnsError()
    {
        var state = ValidState();
        state.Title = "";
        ManifestValidator.Validate(state).Should().Contain(e => e.Contains("title"));
    }

    [Fact]
    public void WhitespaceTitle_ReturnsError()
    {
        var state = ValidState();
        state.Title = "   ";
        ManifestValidator.Validate(state).Should().Contain(e => e.Contains("title"));
    }

    [Fact]
    public void NoModelFiles_ReturnsError()
    {
        var state = ValidState();
        state.ModelFiles.Clear();
        ManifestValidator.Validate(state).Should().Contain(e => e.Contains("model"));
    }

    [Fact]
    public void MissingModelFile_ReturnsError()
    {
        var state = ValidState();
        state.ModelFiles = ["/does/not/exist/model.3mf"];
        ManifestValidator.Validate(state).Should().Contain(e => e.Contains("model.3mf"));
    }

    [Fact]
    public void MissingPhotoFile_ReturnsError()
    {
        var state = ValidState();
        state.Photos = ["/does/not/exist/photo.jpg"];
        ManifestValidator.Validate(state).Should().Contain(e => e.Contains("photo.jpg"));
    }

    [Fact]
    public void PatreonEnabled_FrePostFalse_NoAccessTierId_ReturnsError()
    {
        var state = ValidState();
        state.Platforms.Add(new PlatformState
        {
            PlatformKey = "patreon",
            IsEnabled = true,
            FreePost = false,
            AccessTierId = "",
        });
        ManifestValidator.Validate(state).Should()
            .Contain(e => e.Contains("access_tier_id"));
    }

    [Fact]
    public void PatreonEnabled_FreePostTrue_NoAccessTierId_IsValid()
    {
        var state = ValidState();
        state.Platforms.Add(new PlatformState
        {
            PlatformKey = "patreon",
            IsEnabled = true,
            FreePost = true,
            AccessTierId = "",
        });
        ManifestValidator.Validate(state).Should().BeEmpty();
    }

    [Fact]
    public void AllValid_ReturnsEmpty()
    {
        var dir = Path.GetTempPath();
        var modelPath = Path.Combine(dir, "model.3mf");
        File.WriteAllText(modelPath, "dummy");

        var state = new ManifestEditorState
        {
            Title = "Good Model",
            ManifestDirectory = dir,
            ModelFiles = [modelPath],
        };

        try
        {
            ManifestValidator.Validate(state).Should().BeEmpty();
        }
        finally
        {
            File.Delete(modelPath);
        }
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
dotnet test tests/ModelPublisher.ManifestEditor.Tests/ --filter "ManifestValidatorTests"
```

Expected: compile error (ManifestValidator not found).

- [ ] **Step 3: Implement ManifestValidator.cs**

```csharp
// src/ModelPublisher.ManifestEditor/Models/ManifestValidator.cs
namespace ModelPublisher.ManifestEditor.Models;

public static class ManifestValidator
{
    public static IReadOnlyList<string> Validate(ManifestEditorState state)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(state.Title))
            errors.Add("Title is required.");

        if (state.ModelFiles.Count == 0)
            errors.Add("At least one model file is required.");

        foreach (var path in state.ModelFiles.Where(p => !File.Exists(p)))
            errors.Add($"Model file not found: {Path.GetFileName(path)}");

        foreach (var path in state.Photos.Where(p => !File.Exists(p)))
            errors.Add($"Photo not found: {Path.GetFileName(path)}");

        foreach (var p in state.Platforms.Where(p => p.IsEnabled))
        {
            foreach (var profile in p.PrintProfiles.Where(f => !File.Exists(f)))
                errors.Add($"Print profile not found: {Path.GetFileName(profile)}");

            if (p.PlatformKey == "patreon" && !p.FreePost && string.IsNullOrWhiteSpace(p.AccessTierId))
                errors.Add("Patreon: access_tier_id is required when free_post is false.");
        }

        return errors;
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test tests/ModelPublisher.ManifestEditor.Tests/ --filter "ManifestValidatorTests"
```

Expected: all 8 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/ModelPublisher.ManifestEditor/Models/ManifestValidator.cs tests/ModelPublisher.ManifestEditor.Tests/ManifestValidatorTests.cs
git commit -m "feat: add ManifestValidator with pre-save rules

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 4: FileEntryViewModel + FilesViewModel

**Files:**
- Create: `src/ModelPublisher.ManifestEditor/ViewModels/FileEntryViewModel.cs`
- Create: `src/ModelPublisher.ManifestEditor/ViewModels/FilesViewModel.cs`
- Create: `tests/ModelPublisher.ManifestEditor.Tests/FilesViewModelTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/ModelPublisher.ManifestEditor.Tests/FilesViewModelTests.cs
using FluentAssertions;
using ModelPublisher.ManifestEditor.ViewModels;

namespace ModelPublisher.ManifestEditor.Tests;

public class FilesViewModelTests
{
    [Fact]
    public void AddModelFile_AppendsToList()
    {
        var vm = new FilesViewModel();
        vm.AddModelFile("/a/model.3mf");
        vm.ModelFiles.Should().ContainSingle(e => e.AbsolutePath == "/a/model.3mf");
    }

    [Fact]
    public void RemoveModelFile_RemovesFromList()
    {
        var vm = new FilesViewModel();
        vm.AddModelFile("/a/model.3mf");
        var entry = vm.ModelFiles[0];
        vm.RemoveModelFile(entry);
        vm.ModelFiles.Should().BeEmpty();
    }

    [Fact]
    public void MoveUpModelFile_MovesItemUp()
    {
        var vm = new FilesViewModel();
        vm.AddModelFile("/a.3mf");
        vm.AddModelFile("/b.3mf");
        var b = vm.ModelFiles[1];
        vm.MoveUpModelFile(b);
        vm.ModelFiles[0].AbsolutePath.Should().Be("/b.3mf");
        vm.ModelFiles[1].AbsolutePath.Should().Be("/a.3mf");
    }

    [Fact]
    public void MoveUpModelFile_DoesNothing_WhenFirst()
    {
        var vm = new FilesViewModel();
        vm.AddModelFile("/a.3mf");
        vm.AddModelFile("/b.3mf");
        var a = vm.ModelFiles[0];
        vm.MoveUpModelFile(a);
        vm.ModelFiles[0].AbsolutePath.Should().Be("/a.3mf");
    }

    [Fact]
    public void MoveDownModelFile_MovesItemDown()
    {
        var vm = new FilesViewModel();
        vm.AddModelFile("/a.3mf");
        vm.AddModelFile("/b.3mf");
        var a = vm.ModelFiles[0];
        vm.MoveDownModelFile(a);
        vm.ModelFiles[0].AbsolutePath.Should().Be("/b.3mf");
    }

    [Fact]
    public void MoveDownModelFile_DoesNothing_WhenLast()
    {
        var vm = new FilesViewModel();
        vm.AddModelFile("/a.3mf");
        vm.AddModelFile("/b.3mf");
        var b = vm.ModelFiles[1];
        vm.MoveDownModelFile(b);
        vm.ModelFiles[1].AbsolutePath.Should().Be("/b.3mf");
    }

    [Fact]
    public void CoverOptions_IncludesNoneAndPhotos()
    {
        var vm = new FilesViewModel();
        vm.AddPhoto("/a/cover.jpg");
        vm.CoverOptions.Should().HaveCount(2);
        vm.CoverOptions[0].Should().BeNull();
        vm.CoverOptions[1].Should().Be("/a/cover.jpg");
    }

    [Fact]
    public void LoadFrom_SetsAllFields()
    {
        var vm = new FilesViewModel();
        vm.LoadFrom(
            modelFiles: ["/a/model.3mf"],
            photos: ["/a/cover.jpg"],
            cover: "/a/cover.jpg",
            manifestDir: "/a");

        vm.ModelFiles.Should().ContainSingle(e => e.AbsolutePath == "/a/model.3mf");
        vm.Photos.Should().ContainSingle(e => e.AbsolutePath == "/a/cover.jpg");
        vm.SelectedCover.Should().Be("/a/cover.jpg");
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
dotnet test tests/ModelPublisher.ManifestEditor.Tests/ --filter "FilesViewModelTests"
```

Expected: compile errors.

- [ ] **Step 3: Create FileEntryViewModel.cs**

```csharp
// src/ModelPublisher.ManifestEditor/ViewModels/FileEntryViewModel.cs
namespace ModelPublisher.ManifestEditor.ViewModels;

public class FileEntryViewModel
{
    public string AbsolutePath { get; }
    public bool IsMissing { get; }
    public string DisplayName => Path.GetFileName(AbsolutePath);

    public FileEntryViewModel(string absolutePath, bool isMissing = false)
    {
        AbsolutePath = absolutePath;
        IsMissing = isMissing;
    }
}
```

- [ ] **Step 4: Create FilesViewModel.cs**

```csharp
// src/ModelPublisher.ManifestEditor/ViewModels/FilesViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ModelPublisher.ManifestEditor.ViewModels;

public partial class FilesViewModel : ObservableObject, ISectionViewModel
{
    public string SectionName => "Files";

    public ObservableCollection<FileEntryViewModel> ModelFiles { get; } = [];
    public ObservableCollection<FileEntryViewModel> Photos { get; } = [];

    // null = no explicit cover (use first photo)
    [ObservableProperty]
    private string? _selectedCover;

    // Rebuilt whenever Photos changes
    public ObservableCollection<string?> CoverOptions { get; } = [];

    public void AddModelFile(string absolutePath) =>
        ModelFiles.Add(new FileEntryViewModel(absolutePath, !File.Exists(absolutePath)));

    public void RemoveModelFile(FileEntryViewModel item) => ModelFiles.Remove(item);

    public void MoveUpModelFile(FileEntryViewModel item)
    {
        var i = ModelFiles.IndexOf(item);
        if (i > 0) ModelFiles.Move(i, i - 1);
    }

    public void MoveDownModelFile(FileEntryViewModel item)
    {
        var i = ModelFiles.IndexOf(item);
        if (i >= 0 && i < ModelFiles.Count - 1) ModelFiles.Move(i, i + 1);
    }

    public void AddPhoto(string absolutePath)
    {
        Photos.Add(new FileEntryViewModel(absolutePath, !File.Exists(absolutePath)));
        RebuildCoverOptions();
    }

    // RemovePhoto relay command is added in Task 10 as a [RelayCommand]; not needed here.

    public void MoveUpPhoto(FileEntryViewModel item)
    {
        var i = Photos.IndexOf(item);
        if (i > 0) Photos.Move(i, i - 1);
    }

    public void MoveDownPhoto(FileEntryViewModel item)
    {
        var i = Photos.IndexOf(item);
        if (i >= 0 && i < Photos.Count - 1) Photos.Move(i, i + 1);
    }

    public void LoadFrom(
        IEnumerable<string> modelFiles,
        IEnumerable<string> photos,
        string? cover,
        string manifestDir)
    {
        ModelFiles.Clear();
        Photos.Clear();

        foreach (var f in modelFiles)
            ModelFiles.Add(new FileEntryViewModel(f, !File.Exists(f)));

        foreach (var p in photos)
            Photos.Add(new FileEntryViewModel(p, !File.Exists(p)));

        RebuildCoverOptions();
        SelectedCover = cover;
    }

    public void Clear()
    {
        ModelFiles.Clear();
        Photos.Clear();
        CoverOptions.Clear();
        CoverOptions.Add(null);
        SelectedCover = null;
    }

    private void RebuildCoverOptions()
    {
        CoverOptions.Clear();
        CoverOptions.Add(null); // "none" / use first photo
        foreach (var p in Photos)
            CoverOptions.Add(p.AbsolutePath);
    }
}
```

- [ ] **Step 5: Create ISectionViewModel.cs** (interface all section VMs implement for sidebar display)

```csharp
// src/ModelPublisher.ManifestEditor/ViewModels/ISectionViewModel.cs
namespace ModelPublisher.ManifestEditor.ViewModels;

public interface ISectionViewModel
{
    string SectionName { get; }
}
```

- [ ] **Step 6: Run tests**

```bash
dotnet test tests/ModelPublisher.ManifestEditor.Tests/ --filter "FilesViewModelTests"
```

Expected: all 8 tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/ModelPublisher.ManifestEditor/ViewModels/ tests/ModelPublisher.ManifestEditor.Tests/FilesViewModelTests.cs
git commit -m "feat: add FileEntryViewModel and FilesViewModel

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 5: BasicInfoViewModel

**Files:**
- Create: `src/ModelPublisher.ManifestEditor/ViewModels/BasicInfoViewModel.cs`
- Create: `tests/ModelPublisher.ManifestEditor.Tests/BasicInfoViewModelTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/ModelPublisher.ManifestEditor.Tests/BasicInfoViewModelTests.cs
using FluentAssertions;
using ModelPublisher.ManifestEditor.ViewModels;

namespace ModelPublisher.ManifestEditor.Tests;

public class BasicInfoViewModelTests
{
    [Fact]
    public void AddTag_AppendsTrimmedTag()
    {
        var vm = new BasicInfoViewModel();
        vm.AddTag("  newtag  ");
        vm.Tags.Should().ContainSingle().Which.Should().Be("newtag");
    }

    [Fact]
    public void AddTag_EmptyString_DoesNothing()
    {
        var vm = new BasicInfoViewModel();
        vm.AddTag("");
        vm.Tags.Should().BeEmpty();
    }

    [Fact]
    public void AddTag_Whitespace_DoesNothing()
    {
        var vm = new BasicInfoViewModel();
        vm.AddTag("   ");
        vm.Tags.Should().BeEmpty();
    }

    [Fact]
    public void AddTag_Duplicate_DoesNothing()
    {
        var vm = new BasicInfoViewModel();
        vm.AddTag("foo");
        vm.AddTag("foo");
        vm.Tags.Should().ContainSingle();
    }

    [Fact]
    public void RemoveTag_RemovesExistingTag()
    {
        var vm = new BasicInfoViewModel();
        vm.AddTag("foo");
        vm.RemoveTag("foo");
        vm.Tags.Should().BeEmpty();
    }

    [Fact]
    public void LoadFrom_SetsAllFields()
    {
        var vm = new BasicInfoViewModel();
        vm.LoadFrom("My Model", ["tag1", "tag2"], "MIT");
        vm.Title.Should().Be("My Model");
        vm.Tags.Should().Equal("tag1", "tag2");
        vm.License.Should().Be("MIT");
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
dotnet test tests/ModelPublisher.ManifestEditor.Tests/ --filter "BasicInfoViewModelTests"
```

Expected: compile errors.

- [ ] **Step 3: Create BasicInfoViewModel.cs**

```csharp
// src/ModelPublisher.ManifestEditor/ViewModels/BasicInfoViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ModelPublisher.ManifestEditor.ViewModels;

public partial class BasicInfoViewModel : ObservableObject, ISectionViewModel
{
    public string SectionName => "Basic Info";

    public static readonly string[] LicenseOptions =
    [
        "CC-BY-4.0", "CC-BY-SA-4.0", "CC-BY-NC-4.0", "CC-BY-NC-SA-4.0",
        "CC0-1.0", "MIT", "GPL-3.0-only",
    ];

    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private string _license = "CC-BY-4.0";

    // Bound to the TextBox used for adding a new tag
    [ObservableProperty]
    private string _newTagText = "";

    public ObservableCollection<string> Tags { get; } = [];

    public void AddTag(string tag)
    {
        tag = tag.Trim();
        if (string.IsNullOrEmpty(tag)) return;
        if (Tags.Contains(tag)) return;
        Tags.Add(tag);
    }

    public void RemoveTag(string tag) => Tags.Remove(tag);

    public void LoadFrom(string title, IEnumerable<string> tags, string license)
    {
        Title = title;
        License = license;
        Tags.Clear();
        foreach (var t in tags) Tags.Add(t);
    }

    public void Clear()
    {
        Title = "";
        License = "CC-BY-4.0";
        Tags.Clear();
        NewTagText = "";
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test tests/ModelPublisher.ManifestEditor.Tests/ --filter "BasicInfoViewModelTests"
```

Expected: all 6 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/ModelPublisher.ManifestEditor/ViewModels/BasicInfoViewModel.cs tests/ModelPublisher.ManifestEditor.Tests/BasicInfoViewModelTests.cs
git commit -m "feat: add BasicInfoViewModel with tag management

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 6: PlatformEntryViewModel + PlatformsViewModel

**Files:**
- Create: `src/ModelPublisher.ManifestEditor/ViewModels/PlatformEntryViewModel.cs`
- Create: `src/ModelPublisher.ManifestEditor/ViewModels/PlatformsViewModel.cs`
- Create: `tests/ModelPublisher.ManifestEditor.Tests/PlatformsViewModelTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/ModelPublisher.ManifestEditor.Tests/PlatformsViewModelTests.cs
using FluentAssertions;
using ModelPublisher.ManifestEditor.ViewModels;

namespace ModelPublisher.ManifestEditor.Tests;

public class PlatformsViewModelTests
{
    [Fact]
    public void Constructor_CreatesAllSixPlatforms()
    {
        var vm = new PlatformsViewModel();
        vm.Entries.Select(e => e.PlatformKey)
            .Should().Equal("printables", "makerworld", "cults3d", "thangs", "makeronline", "patreon");
    }

    [Fact]
    public void AllDisabledByDefault()
    {
        var vm = new PlatformsViewModel();
        vm.Entries.Should().AllSatisfy(e => e.IsEnabled.Should().BeFalse());
    }

    [Fact]
    public void IsPatreon_TrueOnlyForPatreon()
    {
        var vm = new PlatformsViewModel();
        vm.Entries.Where(e => e.IsPatreon).Should().ContainSingle(e => e.PlatformKey == "patreon");
        vm.Entries.Where(e => !e.IsPatreon).Should().HaveCount(5);
    }

    [Fact]
    public void TierOptions_ContainsFreeAndPremium()
    {
        var vm = new PlatformsViewModel();
        vm.Entries[0].TierOptions.Should().Equal("free", "premium");
    }

    [Fact]
    public void LoadFrom_SetsEnabledAndConfigFields()
    {
        var vm = new PlatformsViewModel();
        vm.LoadFrom(
        [
            new Models.PlatformState { PlatformKey = "printables", IsEnabled = true, Tier = "premium" },
            new Models.PlatformState { PlatformKey = "makerworld", IsEnabled = false, Tier = "free" },
            new Models.PlatformState { PlatformKey = "cults3d",    IsEnabled = false, Tier = "free" },
            new Models.PlatformState { PlatformKey = "thangs",     IsEnabled = false, Tier = "free" },
            new Models.PlatformState { PlatformKey = "makeronline",IsEnabled = false, Tier = "free" },
            new Models.PlatformState
            {
                PlatformKey = "patreon", IsEnabled = true,
                Tier = "free", FreePost = false, AccessTierId = "tier_abc"
            },
        ], manifestDir: "/some/dir");

        var printables = vm.Entries.First(e => e.PlatformKey == "printables");
        printables.IsEnabled.Should().BeTrue();
        printables.Tier.Should().Be("premium");

        var patreon = vm.Entries.First(e => e.PlatformKey == "patreon");
        patreon.IsEnabled.Should().BeTrue();
        patreon.FreePost.Should().BeFalse();
        patreon.AccessTierId.Should().Be("tier_abc");
    }

    [Fact]
    public void ToPlatformStates_OnlyReturnsAllSix_WithEnabledFlag()
    {
        var vm = new PlatformsViewModel();
        vm.Entries.First(e => e.PlatformKey == "thangs").IsEnabled = true;

        var states = vm.ToPlatformStates();
        states.Should().HaveCount(6);
        states.First(s => s.PlatformKey == "thangs").IsEnabled.Should().BeTrue();
        states.First(s => s.PlatformKey == "printables").IsEnabled.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
dotnet test tests/ModelPublisher.ManifestEditor.Tests/ --filter "PlatformsViewModelTests"
```

Expected: compile errors.

- [ ] **Step 3: Create PlatformEntryViewModel.cs**

```csharp
// src/ModelPublisher.ManifestEditor/ViewModels/PlatformEntryViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ModelPublisher.ManifestEditor.Models;

namespace ModelPublisher.ManifestEditor.ViewModels;

public partial class PlatformEntryViewModel : ObservableObject
{
    public static readonly string[] TierOptions = ["free", "premium"];

    public string PlatformKey { get; }
    public string PlatformName { get; }
    public bool IsPatreon => PlatformKey == "patreon";

    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private string _tier = "free";
    [ObservableProperty] private bool _freePost = true;
    [ObservableProperty] private string _accessTierId = "";

    public ObservableCollection<FileEntryViewModel> PrintProfiles { get; } = [];

    private static readonly Dictionary<string, string> PlatformNames = new()
    {
        ["printables"]  = "Printables",
        ["makerworld"]  = "MakerWorld",
        ["cults3d"]     = "Cults3D",
        ["thangs"]      = "Thangs",
        ["makeronline"] = "MakerOnline",
        ["patreon"]     = "Patreon",
    };

    public PlatformEntryViewModel(string platformKey)
    {
        PlatformKey = platformKey;
        PlatformName = PlatformNames.GetValueOrDefault(platformKey, platformKey);
    }

    public void LoadFrom(PlatformState state, string manifestDir)
    {
        IsEnabled = state.IsEnabled;
        Tier = state.Tier;
        FreePost = state.FreePost;
        AccessTierId = state.AccessTierId;
        PrintProfiles.Clear();
        foreach (var p in state.PrintProfiles)
            PrintProfiles.Add(new FileEntryViewModel(p, !File.Exists(p)));
    }

    public PlatformState ToState() => new()
    {
        PlatformKey = PlatformKey,
        IsEnabled = IsEnabled,
        Tier = Tier,
        PrintProfiles = PrintProfiles.Select(p => p.AbsolutePath).ToList(),
        FreePost = FreePost,
        AccessTierId = AccessTierId,
    };

    public void AddPrintProfile(string absolutePath) =>
        PrintProfiles.Add(new FileEntryViewModel(absolutePath, !File.Exists(absolutePath)));

    public void RemovePrintProfile(FileEntryViewModel item) => PrintProfiles.Remove(item);
}
```

- [ ] **Step 4: Create PlatformsViewModel.cs**

```csharp
// src/ModelPublisher.ManifestEditor/ViewModels/PlatformsViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ModelPublisher.ManifestEditor.Models;

namespace ModelPublisher.ManifestEditor.ViewModels;

public partial class PlatformsViewModel : ObservableObject, ISectionViewModel
{
    public string SectionName => "Platforms";

    public ObservableCollection<PlatformEntryViewModel> Entries { get; } = [];

    public PlatformsViewModel()
    {
        foreach (var key in ManifestEditorState.AllPlatformKeys)
            Entries.Add(new PlatformEntryViewModel(key));
    }

    public void LoadFrom(IEnumerable<PlatformState> states, string manifestDir)
    {
        var stateMap = states.ToDictionary(s => s.PlatformKey);
        foreach (var entry in Entries)
        {
            if (stateMap.TryGetValue(entry.PlatformKey, out var state))
                entry.LoadFrom(state, manifestDir);
        }
    }

    public List<PlatformState> ToPlatformStates() =>
        Entries.Select(e => e.ToState()).ToList();

    public void Clear()
    {
        foreach (var e in Entries)
            e.LoadFrom(new PlatformState { PlatformKey = e.PlatformKey }, manifestDir: "");
    }
}
```

- [ ] **Step 5: Run tests**

```bash
dotnet test tests/ModelPublisher.ManifestEditor.Tests/ --filter "PlatformsViewModelTests"
```

Expected: all 6 tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/ModelPublisher.ManifestEditor/ViewModels/PlatformEntryViewModel.cs src/ModelPublisher.ManifestEditor/ViewModels/PlatformsViewModel.cs tests/ModelPublisher.ManifestEditor.Tests/PlatformsViewModelTests.cs
git commit -m "feat: add PlatformEntryViewModel and PlatformsViewModel

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 7: DescriptionViewModel + MainWindowViewModel

**Files:**
- Create: `src/ModelPublisher.ManifestEditor/ViewModels/DescriptionViewModel.cs`
- Create: `src/ModelPublisher.ManifestEditor/ViewModels/MainWindowViewModel.cs`

- [ ] **Step 1: Create DescriptionViewModel.cs**

```csharp
// src/ModelPublisher.ManifestEditor/ViewModels/DescriptionViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace ModelPublisher.ManifestEditor.ViewModels;

public partial class DescriptionViewModel : ObservableObject, ISectionViewModel
{
    public string SectionName => "Description";

    [ObservableProperty]
    private string _text = "";
}
```

- [ ] **Step 2: Create MainWindowViewModel.cs**

```csharp
// src/ModelPublisher.ManifestEditor/ViewModels/MainWindowViewModel.cs
using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModelPublisher.Core.Models;
using ModelPublisher.ManifestEditor.Models;
using ModelPublisher.ManifestEditor.Services;

namespace ModelPublisher.ManifestEditor.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IFileDialogService _dialogs;

    public BasicInfoViewModel    BasicInfo   { get; } = new();
    public FilesViewModel        Files       { get; } = new();
    public DescriptionViewModel  Description { get; } = new();
    public PlatformsViewModel    Platforms   { get; } = new();

    public ObservableCollection<ISectionViewModel> Sections { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private ISectionViewModel _activeSection;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isDirty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private string _manifestDirectory = "";

    public string WindowTitle =>
        $"Manifest Editor{(string.IsNullOrEmpty(ManifestDirectory) ? "" : $" — {Path.GetFileName(ManifestDirectory)}")}{(IsDirty ? " *" : "")}";

    public bool CanSave => !string.IsNullOrEmpty(ManifestDirectory);

    public MainWindowViewModel(IFileDialogService dialogs)
    {
        _dialogs = dialogs;
        Sections = [BasicInfo, Files, Description, Platforms];
        _activeSection = BasicInfo;
        SubscribeDirtyTracking();
    }

    private void SubscribeDirtyTracking()
    {
        BasicInfo.PropertyChanged    += (_, _) => IsDirty = true;
        BasicInfo.Tags.CollectionChanged += (_, _) => IsDirty = true;
        Files.ModelFiles.CollectionChanged += (_, _) => IsDirty = true;
        Files.Photos.CollectionChanged     += (_, _) => IsDirty = true;
        Files.PropertyChanged              += (_, _) => IsDirty = true;
        Description.PropertyChanged        += (_, _) => IsDirty = true;
        foreach (var entry in Platforms.Entries)
        {
            entry.PropertyChanged += (_, _) => IsDirty = true;
            entry.PrintProfiles.CollectionChanged += (_, _) => IsDirty = true;
        }
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        if (IsDirty)
        {
            var save = await _dialogs.ConfirmUnsavedChangesAsync();
            if (save) await SaveAsync();
        }

        var path = await _dialogs.OpenManifestLocationAsync();
        if (path is null) return;

        string manifestPath;
        string dir;

        if (Directory.Exists(path))
        {
            dir = path;
            manifestPath = Path.Combine(dir, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                // New manifest — blank state
                LoadBlankState(dir);
                return;
            }
        }
        else
        {
            manifestPath = path;
            dir = Path.GetDirectoryName(path)!;
        }

        try
        {
            var json = await File.ReadAllTextAsync(manifestPath);
            var manifest = JsonSerializer.Deserialize<ReleaseManifest>(json);
            if (manifest is null) throw new JsonException("Deserialized to null.");
            manifest.ManifestDirectory = dir;
            LoadFromManifest(manifest);
        }
        catch (Exception ex)
        {
            await _dialogs.ShowErrorAsync("Could not open manifest", ex.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        var state = BuildState();
        var errors = ManifestValidator.Validate(state);
        if (errors.Count > 0)
        {
            await _dialogs.ShowValidationErrorsAsync(errors);
            return;
        }

        try
        {
            var json = state.ToJson();
            var path = Path.Combine(ManifestDirectory, "manifest.json");
            await File.WriteAllTextAsync(path, json);
            IsDirty = false;
        }
        catch (Exception ex)
        {
            await _dialogs.ShowErrorAsync("Save failed", ex.Message);
        }
    }

    private void LoadFromManifest(ReleaseManifest manifest)
    {
        var state = ManifestEditorState.FromManifest(manifest);
        ManifestDirectory = state.ManifestDirectory;
        BasicInfo.LoadFrom(state.Title, state.Tags, state.License);
        Files.LoadFrom(state.ModelFiles, state.Photos, state.Cover, state.ManifestDirectory);
        Description.Text = state.Description;
        Platforms.LoadFrom(state.Platforms, state.ManifestDirectory);
        IsDirty = false;
    }

    private void LoadBlankState(string dir)
    {
        ManifestDirectory = dir;
        BasicInfo.Clear();
        Files.Clear();
        Description.Text = "";
        Platforms.Clear();
        IsDirty = false;
    }

    private ManifestEditorState BuildState() => new()
    {
        Title = BasicInfo.Title,
        Description = Description.Text,
        Tags = BasicInfo.Tags.ToList(),
        License = BasicInfo.License,
        ManifestDirectory = ManifestDirectory,
        ModelFiles = Files.ModelFiles.Select(e => e.AbsolutePath).ToList(),
        Photos = Files.Photos.Select(e => e.AbsolutePath).ToList(),
        Cover = Files.SelectedCover,
        Platforms = Platforms.ToPlatformStates(),
    };
}
```

- [ ] **Step 3: Build to confirm it compiles**

```bash
dotnet build src/ModelPublisher.ManifestEditor/ModelPublisher.ManifestEditor.csproj
```

Expected: Build succeeded. If `AvaloniaFileDialogService` is referenced anywhere, stub it first (it's created in Task 13).

- [ ] **Step 4: Commit**

```bash
git add src/ModelPublisher.ManifestEditor/ViewModels/
git commit -m "feat: add DescriptionViewModel and MainWindowViewModel

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 8: MainWindow Shell AXAML

**Files:**
- Modify: `src/ModelPublisher.ManifestEditor/Views/MainWindow.axaml`
- Modify: `src/ModelPublisher.ManifestEditor/Views/MainWindow.axaml.cs`

The shell has a 160px left sidebar and a content area on the right. The `ContentControl` uses `DataTemplates` to swap views based on which section ViewModel is active.

- [ ] **Step 1: Create stub section views** so the DataTemplates can reference them (full AXAML comes in later tasks):

Create `Views/BasicInfoView.axaml`:
```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="ModelPublisher.ManifestEditor.Views.BasicInfoView">
    <TextBlock Text="Basic Info" />
</UserControl>
```

Create `Views/BasicInfoView.axaml.cs`:
```csharp
using Avalonia.Controls;
namespace ModelPublisher.ManifestEditor.Views;
public partial class BasicInfoView : UserControl { }
```

Repeat for `FilesView`, `DescriptionView`, `PlatformsView` with matching stubs (`Text="Files"`, `Text="Description"`, `Text="Platforms"`).

- [ ] **Step 2: Replace MainWindow.axaml with the shell layout**

```xml
<!-- src/ModelPublisher.ManifestEditor/Views/MainWindow.axaml -->
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:ModelPublisher.ManifestEditor.ViewModels"
        xmlns:views="using:ModelPublisher.ManifestEditor.Views"
        x:Class="ModelPublisher.ManifestEditor.Views.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Title="{Binding WindowTitle}"
        Width="960" Height="680"
        MinWidth="720" MinHeight="500">

    <Grid ColumnDefinitions="160,*">

        <!-- Sidebar -->
        <DockPanel Grid.Column="0" Background="#0d0d0d">
            <!-- Open + Save at the bottom -->
            <StackPanel DockPanel.Dock="Bottom" Margin="8,0,8,12" Spacing="6">
                <Button Content="Save"
                        Command="{Binding SaveCommand}"
                        IsEnabled="{Binding CanSave}"
                        HorizontalAlignment="Stretch"
                        HorizontalContentAlignment="Center" />
                <Button Content="Open..."
                        Command="{Binding OpenCommand}"
                        HorizontalAlignment="Stretch"
                        HorizontalContentAlignment="Center" />
            </StackPanel>

            <!-- Section nav -->
            <ListBox ItemsSource="{Binding Sections}"
                     SelectedItem="{Binding ActiveSection}"
                     Background="Transparent"
                     BorderThickness="0"
                     Margin="4,8,4,0">
                <ListBox.ItemTemplate>
                    <DataTemplate>
                        <TextBlock Text="{Binding SectionName}" Padding="4,2" />
                    </DataTemplate>
                </ListBox.ItemTemplate>
            </ListBox>
        </DockPanel>

        <!-- Content area -->
        <Border Grid.Column="1" BorderBrush="#2a2a2a" BorderThickness="1,0,0,0">
            <ContentControl Content="{Binding ActiveSection}" Margin="20">
                <ContentControl.DataTemplates>
                    <DataTemplate DataType="vm:BasicInfoViewModel">
                        <views:BasicInfoView />
                    </DataTemplate>
                    <DataTemplate DataType="vm:FilesViewModel">
                        <views:FilesView />
                    </DataTemplate>
                    <DataTemplate DataType="vm:DescriptionViewModel">
                        <views:DescriptionView />
                    </DataTemplate>
                    <DataTemplate DataType="vm:PlatformsViewModel">
                        <views:PlatformsView />
                    </DataTemplate>
                </ContentControl.DataTemplates>
            </ContentControl>
        </Border>
    </Grid>
</Window>
```

- [ ] **Step 3: Wire the DataContext in App.axaml.cs** (requires a stub `AvaloniaFileDialogService` -- create it now):

```csharp
// src/ModelPublisher.ManifestEditor/Services/AvaloniaFileDialogService.cs
namespace ModelPublisher.ManifestEditor.Services;

// Stub — full implementation in Task 13
public class AvaloniaFileDialogService : IFileDialogService
{
    public Task<string?> OpenManifestLocationAsync() => Task.FromResult<string?>(null);
    public Task<IReadOnlyList<string>> OpenFilesAsync(string title, params string[] extensions)
        => Task.FromResult<IReadOnlyList<string>>([]);
    public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
    public Task<bool> ConfirmUnsavedChangesAsync() => Task.FromResult(false);
    public Task ShowValidationErrorsAsync(IReadOnlyList<string> errors) => Task.CompletedTask;
}
```

Update `App.axaml.cs`:
```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ModelPublisher.ManifestEditor.Services;
using ModelPublisher.ManifestEditor.ViewModels;
using ModelPublisher.ManifestEditor.Views;

namespace ModelPublisher.ManifestEditor;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(new AvaloniaFileDialogService())
            };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
```

- [ ] **Step 4: Build and run -- confirm the window opens with a working sidebar**

```bash
dotnet run --project src/ModelPublisher.ManifestEditor/
```

Expected: window opens, sidebar shows "Basic Info / Files / Description / Platforms", clicking them switches the content label.

- [ ] **Step 5: Commit**

```bash
git add src/ModelPublisher.ManifestEditor/
git commit -m "feat: add MainWindow shell with sidebar nav

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 9: BasicInfoView

**Files:**
- Modify: `src/ModelPublisher.ManifestEditor/Views/BasicInfoView.axaml`

- [ ] **Step 1: Replace BasicInfoView.axaml**

```xml
<!-- src/ModelPublisher.ManifestEditor/Views/BasicInfoView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:ModelPublisher.ManifestEditor.ViewModels"
             x:Class="ModelPublisher.ManifestEditor.Views.BasicInfoView"
             x:DataType="vm:BasicInfoViewModel">

    <StackPanel Spacing="16">
        <StackPanel Spacing="4">
            <TextBlock Text="Title" FontWeight="SemiBold" />
            <TextBox Text="{Binding Title}" Watermark="Model title (required)" />
        </StackPanel>

        <StackPanel Spacing="4">
            <TextBlock Text="Tags" FontWeight="SemiBold" />
            <!-- Tag chips + add field -->
            <ItemsControl ItemsSource="{Binding Tags}">
                <ItemsControl.ItemsPanel>
                    <ItemsPanelTemplate>
                        <WrapPanel Orientation="Horizontal" />
                    </ItemsPanelTemplate>
                </ItemsControl.ItemsPanel>
                <ItemsControl.ItemTemplate>
                    <DataTemplate x:DataType="x:String">
                        <Border Background="#1e3a5f" CornerRadius="12"
                                Padding="8,3" Margin="0,0,4,4">
                            <StackPanel Orientation="Horizontal" Spacing="4">
                                <TextBlock Text="{Binding}" VerticalAlignment="Center" />
                                <Button Content="×" Padding="0"
                                        Background="Transparent" BorderThickness="0"
                                        FontSize="14" VerticalAlignment="Center"
                                        Command="{Binding $parent[UserControl].((vm:BasicInfoViewModel)DataContext).RemoveTagCommand}"
                                        CommandParameter="{Binding}" />
                            </StackPanel>
                        </Border>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
            <!-- Add tag input -->
            <StackPanel Orientation="Horizontal" Spacing="6">
                <TextBox Text="{Binding NewTagText}"
                         Watermark="Add tag..."
                         MinWidth="150"
                         KeyDown="TagInput_KeyDown" />
                <Button Content="Add" Command="{Binding AddTagCommand}" />
            </StackPanel>
        </StackPanel>

        <StackPanel Spacing="4">
            <TextBlock Text="License" FontWeight="SemiBold" />
            <ComboBox ItemsSource="{Binding LicenseOptions}"
                      SelectedItem="{Binding License}"
                      MinWidth="200" />
        </StackPanel>
    </StackPanel>
</UserControl>
```

- [ ] **Step 2: Add RelayCommands and code-behind to BasicInfoViewModel and BasicInfoView.axaml.cs**

Add to `BasicInfoViewModel.cs` (inside the class, after the existing methods):

```csharp
[RelayCommand]
private void AddTag() => AddTag(NewTagText);

// Note: AddTag(string) already exists -- rename the parameterless relay version to avoid collision:
// Actually: rename the existing public void AddTag(string) to internal and expose only via command.
// Simpler: just add the relay command that calls AddTag(string) and clears the text box.
```

The cleanest fix: rename `AddTag(string tag)` to `SubmitTag(string tag)` and update both the command and tests, OR keep `AddTag(string)` public (for tests) and name the relay command differently. Use this approach -- keep `AddTag(string)` as the testable method, add a relay command that delegates and clears:

```csharp
[RelayCommand]
private void SubmitNewTag()
{
    AddTag(NewTagText);
    NewTagText = "";
}

[RelayCommand]
private void RemoveTag(string tag) => Tags.Remove(tag);
```

Update `BasicInfoView.axaml` to use `SubmitNewTagCommand` instead of `AddTagCommand`, and wire the button to `SubmitNewTagCommand`.

Update `BasicInfoView.axaml.cs`:
```csharp
using Avalonia.Controls;
using Avalonia.Input;
using ModelPublisher.ManifestEditor.ViewModels;

namespace ModelPublisher.ManifestEditor.Views;

public partial class BasicInfoView : UserControl
{
    private void TagInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is BasicInfoViewModel vm)
        {
            vm.SubmitNewTagCommand.Execute(null);
            e.Handled = true;
        }
    }
}
```

Also update `BasicInfoViewModel.cs` `LicenseOptions` to be an instance property (not static) so Avalonia binding works:

```csharp
public string[] LicenseOptions { get; } =
[
    "CC-BY-4.0", "CC-BY-SA-4.0", "CC-BY-NC-4.0", "CC-BY-NC-SA-4.0",
    "CC0-1.0", "MIT", "GPL-3.0-only",
];
```

- [ ] **Step 3: Run the app and verify Basic Info section**

```bash
dotnet run --project src/ModelPublisher.ManifestEditor/
```

Expected: Basic Info section shows title textbox, tag chips with remove, add-tag input, license dropdown.

- [ ] **Step 4: Commit**

```bash
git add src/ModelPublisher.ManifestEditor/Views/BasicInfoView.axaml src/ModelPublisher.ManifestEditor/Views/BasicInfoView.axaml.cs src/ModelPublisher.ManifestEditor/ViewModels/BasicInfoViewModel.cs
git commit -m "feat: implement BasicInfoView with tag chips

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 10: FilesView

**Files:**
- Modify: `src/ModelPublisher.ManifestEditor/Views/FilesView.axaml`
- Modify: `src/ModelPublisher.ManifestEditor/Views/FilesView.axaml.cs`

FilesView needs relay commands on FilesViewModel for the file picker (add buttons) and for adding photos. These require `IFileDialogService`, but `FilesViewModel` doesn't have access to the dialog service directly. Pass commands down from `MainWindowViewModel` via the view's code-behind, or add file-picker relay commands to `FilesViewModel` that accept the service via constructor injection.

The simplest approach: add `AddModelFileCommand` and `AddPhotoCommand` as async relay commands on `MainWindowViewModel` (it already has `_dialogs`), then bind them via a `RelativeSource` in the template to the parent window's DataContext. However, the content area's `DataContext` is the section ViewModel. Use the `TopLevel` from the window to get the storage provider, and inject the dialog service into `FilesViewModel` at construction time.

**Simplest solution:** Pass `IFileDialogService` to `FilesViewModel` constructor. Update `MainWindowViewModel` to pass it in.

- [ ] **Step 1: Add dialog service to FilesViewModel**

Update `FilesViewModel.cs` constructor and add async commands:

```csharp
private readonly IFileDialogService _dialogs;

public FilesViewModel(IFileDialogService dialogs)
{
    _dialogs = dialogs;
}

// Parameterless constructor for tests (no dialogs needed for unit tests)
public FilesViewModel() : this(new NullFileDialogService()) { }

[RelayCommand]
private async Task AddModelFileAsync()
{
    var paths = await _dialogs.OpenFilesAsync("Select model files", ".3mf", ".stl", ".obj", ".zip");
    foreach (var p in paths) AddModelFile(p);
}

[RelayCommand]
private async Task AddPhotoAsync()
{
    var paths = await _dialogs.OpenFilesAsync("Select photos", ".jpg", ".jpeg", ".png", ".webp");
    foreach (var p in paths) AddPhoto(p);
}
```

Add `NullFileDialogService` to `Services/`:

```csharp
// src/ModelPublisher.ManifestEditor/Services/NullFileDialogService.cs
namespace ModelPublisher.ManifestEditor.Services;

internal class NullFileDialogService : IFileDialogService
{
    public Task<string?> OpenManifestLocationAsync() => Task.FromResult<string?>(null);
    public Task<IReadOnlyList<string>> OpenFilesAsync(string title, params string[] extensions)
        => Task.FromResult<IReadOnlyList<string>>([]);
    public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
    public Task<bool> ConfirmUnsavedChangesAsync() => Task.FromResult(false);
    public Task ShowValidationErrorsAsync(IReadOnlyList<string> errors) => Task.CompletedTask;
}
```

Update `MainWindowViewModel` -- change `Files` and `Platforms` from auto-initialized to constructor-initialized and pass the service:

Similarly add `AddPrintProfileAsync` relay command to `PlatformEntryViewModel` -- but it also needs a dialog service. Pass it into each `PlatformEntryViewModel` in `PlatformsViewModel`. Update `PlatformsViewModel` to accept the service and pass it to each entry.

```csharp
// PlatformsViewModel.cs -- updated constructor
private readonly IFileDialogService _dialogs;

public PlatformsViewModel() : this(new NullFileDialogService()) { }

public PlatformsViewModel(IFileDialogService dialogs)
{
    _dialogs = dialogs;
    foreach (var key in ManifestEditorState.AllPlatformKeys)
        Entries.Add(new PlatformEntryViewModel(key, dialogs));
}
```

```csharp
// PlatformEntryViewModel.cs -- updated constructor
private readonly IFileDialogService _dialogs;

public PlatformEntryViewModel(string platformKey) : this(platformKey, new NullFileDialogService()) { }

public PlatformEntryViewModel(string platformKey, IFileDialogService dialogs)
{
    PlatformKey = platformKey;
    PlatformName = PlatformNames.GetValueOrDefault(platformKey, platformKey);
    _dialogs = dialogs;
}

[RelayCommand]
private async Task AddPrintProfileAsync()
{
    var paths = await _dialogs.OpenFilesAsync("Select print profile", ".3mf");
    foreach (var p in paths) AddPrintProfile(p);
}
```

Update `MainWindowViewModel` -- change `Files` and `Platforms` to constructor-initialized properties and pass the service:

```csharp
// Replace the two auto-initialized property declarations:
public FilesViewModel        Files       { get; }   // was: = new()
public PlatformsViewModel    Platforms   { get; }   // was: = new()

// Replace the constructor with:
public MainWindowViewModel(IFileDialogService dialogs)
{
    _dialogs = dialogs;
    Files = new FilesViewModel(dialogs);
    Platforms = new PlatformsViewModel(dialogs);
    Sections = [BasicInfo, Files, Description, Platforms];
    _activeSection = BasicInfo;
    SubscribeDirtyTracking();
}
```

- [ ] **Step 2: Write FilesView.axaml**

```xml
<!-- src/ModelPublisher.ManifestEditor/Views/FilesView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:ModelPublisher.ManifestEditor.ViewModels"
             x:Class="ModelPublisher.ManifestEditor.Views.FilesView"
             x:DataType="vm:FilesViewModel">

    <ScrollViewer>
        <StackPanel Spacing="20">

            <!-- Model Files -->
            <StackPanel Spacing="6">
                <TextBlock Text="Model Files" FontWeight="SemiBold" />
                <ItemsControl ItemsSource="{Binding ModelFiles}"
                              x:Name="ModelFilesList">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate x:DataType="vm:FileEntryViewModel">
                            <Grid ColumnDefinitions="*,32,32,32" Margin="0,0,0,4">
                                <TextBlock Grid.Column="0"
                                           Text="{Binding DisplayName}"
                                           Foreground="{Binding IsMissing, Converter={StaticResource MissingFileColorConverter}}"
                                           VerticalAlignment="Center" />
                                <Button Grid.Column="1" Content="↑" Padding="4,2"
                                        Command="{Binding $parent[UserControl].((vm:FilesViewModel)DataContext).MoveUpModelFileCommand}"
                                        CommandParameter="{Binding}" />
                                <Button Grid.Column="2" Content="↓" Padding="4,2"
                                        Command="{Binding $parent[UserControl].((vm:FilesViewModel)DataContext).MoveDownModelFileCommand}"
                                        CommandParameter="{Binding}" />
                                <Button Grid.Column="3" Content="✕" Padding="4,2"
                                        Command="{Binding $parent[UserControl].((vm:FilesViewModel)DataContext).RemoveModelFileCommand}"
                                        CommandParameter="{Binding}" />
                            </Grid>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
                <Button Content="Add model file..." Command="{Binding AddModelFileCommand}" />
            </StackPanel>

            <!-- Photos -->
            <StackPanel Spacing="6">
                <TextBlock Text="Photos" FontWeight="SemiBold" />
                <ItemsControl ItemsSource="{Binding Photos}">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate x:DataType="vm:FileEntryViewModel">
                            <Grid ColumnDefinitions="*,32,32,32" Margin="0,0,0,4">
                                <TextBlock Grid.Column="0"
                                           Text="{Binding DisplayName}"
                                           Foreground="{Binding IsMissing, Converter={StaticResource MissingFileColorConverter}}"
                                           VerticalAlignment="Center" />
                                <Button Grid.Column="1" Content="↑" Padding="4,2"
                                        Command="{Binding $parent[UserControl].((vm:FilesViewModel)DataContext).MoveUpPhotoCommand}"
                                        CommandParameter="{Binding}" />
                                <Button Grid.Column="2" Content="↓" Padding="4,2"
                                        Command="{Binding $parent[UserControl].((vm:FilesViewModel)DataContext).MoveDownPhotoCommand}"
                                        CommandParameter="{Binding}" />
                                <Button Grid.Column="3" Content="✕" Padding="4,2"
                                        Command="{Binding $parent[UserControl].((vm:FilesViewModel)DataContext).RemovePhotoCommand}"
                                        CommandParameter="{Binding}" />
                            </Grid>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
                <Button Content="Add photo..." Command="{Binding AddPhotoCommand}" />
            </StackPanel>

            <!-- Cover -->
            <StackPanel Spacing="4">
                <TextBlock Text="Cover photo" FontWeight="SemiBold" />
                <TextBlock Text="(optional — defaults to first photo)" Foreground="#666" FontSize="11" />
                <ComboBox ItemsSource="{Binding CoverOptions}"
                          SelectedItem="{Binding SelectedCover}"
                          MinWidth="250">
                    <ComboBox.ItemTemplate>
                        <DataTemplate x:DataType="x:String">
                            <TextBlock Text="{Binding, FallbackValue='(none — use first photo)'}" />
                        </DataTemplate>
                    </ComboBox.ItemTemplate>
                </ComboBox>
            </StackPanel>

        </StackPanel>
    </ScrollViewer>
</UserControl>
```

Add relay commands for Remove/MoveUp/MoveDown to `FilesViewModel` (they take a parameter but CommunityToolkit generates parameterized relay commands when the method has a parameter):

```csharp
[RelayCommand] private void RemoveModelFile(FileEntryViewModel item) => ModelFiles.Remove(item);
[RelayCommand] private void MoveUpModelFile(FileEntryViewModel item) { var i = ModelFiles.IndexOf(item); if (i > 0) ModelFiles.Move(i, i - 1); }
[RelayCommand] private void MoveDownModelFile(FileEntryViewModel item) { var i = ModelFiles.IndexOf(item); if (i >= 0 && i < ModelFiles.Count - 1) ModelFiles.Move(i, i + 1); }
[RelayCommand] private void RemovePhoto(FileEntryViewModel item) { if (SelectedCover == item.AbsolutePath) SelectedCover = null; Photos.Remove(item); RebuildCoverOptions(); }
[RelayCommand] private void MoveUpPhoto(FileEntryViewModel item) { var i = Photos.IndexOf(item); if (i > 0) Photos.Move(i, i - 1); }
[RelayCommand] private void MoveDownPhoto(FileEntryViewModel item) { var i = Photos.IndexOf(item); if (i >= 0 && i < Photos.Count - 1) Photos.Move(i, i + 1); }
```

**Note on `MissingFileColorConverter`:** Register a `BoolToColorConverter` in `App.axaml`:

```xml
<Application.Resources>
    <SolidColorBrush x:Key="MissingFileBrush" Color="#ef4444" />
    <SolidColorBrush x:Key="NormalFileBrush" Color="#d4d4d4" />
    <local:BoolToBrushConverter x:Key="MissingFileColorConverter"
                                TrueBrush="{StaticResource MissingFileBrush}"
                                FalseBrush="{StaticResource NormalFileBrush}" />
</Application.Resources>
```

Create `Converters/BoolToBrushConverter.cs`:

```csharp
// src/ModelPublisher.ManifestEditor/Converters/BoolToBrushConverter.cs
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ModelPublisher.ManifestEditor.Converters;

public class BoolToBrushConverter : IValueConverter
{
    public IBrush? TrueBrush { get; set; }
    public IBrush? FalseBrush { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? TrueBrush : FalseBrush;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
```

Add `xmlns:local="using:ModelPublisher.ManifestEditor.Converters"` to `App.axaml`.

- [ ] **Step 3: Run the app and verify Files section**

```bash
dotnet run --project src/ModelPublisher.ManifestEditor/
```

Expected: Files section shows model files list, photos list (both with ↑/↓/✕ buttons), and cover dropdown.

- [ ] **Step 4: Commit**

```bash
git add src/ModelPublisher.ManifestEditor/
git commit -m "feat: implement FilesView with reorder buttons

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 11: DescriptionView

**Files:**
- Modify: `src/ModelPublisher.ManifestEditor/Views/DescriptionView.axaml`

- [ ] **Step 1: Replace DescriptionView.axaml**

```xml
<!-- src/ModelPublisher.ManifestEditor/Views/DescriptionView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:ModelPublisher.ManifestEditor.ViewModels"
             x:Class="ModelPublisher.ManifestEditor.Views.DescriptionView"
             x:DataType="vm:DescriptionViewModel">

    <Grid RowDefinitions="Auto,*">
        <TextBlock Grid.Row="0" Text="Description (Markdown)"
                   FontWeight="SemiBold" Margin="0,0,0,8" />
        <TextBox Grid.Row="1"
                 Text="{Binding Text}"
                 AcceptsReturn="True"
                 TextWrapping="Wrap"
                 FontFamily="Cascadia Code,Consolas,monospace"
                 FontSize="13"
                 VerticalAlignment="Stretch"
                 VerticalContentAlignment="Top"
                 MinHeight="300" />
    </Grid>
</UserControl>
```

- [ ] **Step 2: Run the app and verify Description section**

```bash
dotnet run --project src/ModelPublisher.ManifestEditor/
```

Expected: Description section shows a tall monospace text editor.

- [ ] **Step 3: Commit**

```bash
git add src/ModelPublisher.ManifestEditor/Views/DescriptionView.axaml
git commit -m "feat: implement DescriptionView with monospace markdown editor

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 12: PlatformsView

**Files:**
- Modify: `src/ModelPublisher.ManifestEditor/Views/PlatformsView.axaml`

- [ ] **Step 1: Replace PlatformsView.axaml**

```xml
<!-- src/ModelPublisher.ManifestEditor/Views/PlatformsView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:ModelPublisher.ManifestEditor.ViewModels"
             x:Class="ModelPublisher.ManifestEditor.Views.PlatformsView"
             x:DataType="vm:PlatformsViewModel">

    <ScrollViewer>
        <ItemsControl ItemsSource="{Binding Entries}">
            <ItemsControl.ItemTemplate>
                <DataTemplate x:DataType="vm:PlatformEntryViewModel">
                    <Border BorderBrush="#2a2a2a" BorderThickness="1"
                            CornerRadius="6" Margin="0,0,0,8">
                        <StackPanel>
                            <!-- Toggle header -->
                            <StackPanel Orientation="Horizontal" Spacing="10"
                                        Margin="12,10,12,10">
                                <CheckBox IsChecked="{Binding IsEnabled}" />
                                <TextBlock Text="{Binding PlatformName}"
                                           FontWeight="SemiBold"
                                           VerticalAlignment="Center" />
                            </StackPanel>

                            <!-- Config (only when enabled) -->
                            <StackPanel IsVisible="{Binding IsEnabled}"
                                        Margin="16,0,16,12" Spacing="10">
                                <Border BorderBrush="#333" BorderThickness="0,1,0,0" />

                                <!-- Tier -->
                                <StackPanel Spacing="4">
                                    <TextBlock Text="Tier" FontSize="12" Foreground="#888" />
                                    <ComboBox ItemsSource="{Binding TierOptions}"
                                              SelectedItem="{Binding Tier}"
                                              MinWidth="160" />
                                </StackPanel>

                                <!-- Print profiles -->
                                <StackPanel Spacing="4">
                                    <TextBlock Text="Print profiles" FontSize="12" Foreground="#888" />
                                    <ItemsControl ItemsSource="{Binding PrintProfiles}">
                                        <ItemsControl.ItemTemplate>
                                            <DataTemplate x:DataType="vm:FileEntryViewModel">
                                                <StackPanel Orientation="Horizontal" Spacing="6" Margin="0,0,0,3">
                                                    <TextBlock Text="{Binding DisplayName}"
                                                               Foreground="{Binding IsMissing, Converter={StaticResource MissingFileColorConverter}}"
                                                               VerticalAlignment="Center" />
                                                    <Button Content="✕" Padding="3,1"
                                                            Command="{Binding $parent[UserControl].((vm:PlatformEntryViewModel)DataContext).RemovePrintProfileCommand}"
                                                            CommandParameter="{Binding}" />
                                                </StackPanel>
                                            </DataTemplate>
                                        </ItemsControl.ItemTemplate>
                                    </ItemsControl>
                                    <Button Content="Add print profile..."
                                            Command="{Binding AddPrintProfileCommand}" />
                                </StackPanel>

                                <!-- Patreon-specific -->
                                <StackPanel IsVisible="{Binding IsPatreon}" Spacing="8">
                                    <Border BorderBrush="#333" BorderThickness="0,1,0,0" />
                                    <StackPanel Spacing="4">
                                        <CheckBox IsChecked="{Binding FreePost}"
                                                  Content="Free post (visible without Patreon subscription)" />
                                    </StackPanel>
                                    <StackPanel Spacing="4" IsVisible="{Binding !FreePost}">
                                        <TextBlock Text="Access Tier ID" FontSize="12" Foreground="#888" />
                                        <TextBox Text="{Binding AccessTierId}"
                                                 Watermark="e.g. tier_abc123"
                                                 MinWidth="260" />
                                    </StackPanel>
                                </StackPanel>

                            </StackPanel>
                        </StackPanel>
                    </Border>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
    </ScrollViewer>
</UserControl>
```

- [ ] **Step 2: Run the app and verify Platforms section**

```bash
dotnet run --project src/ModelPublisher.ManifestEditor/
```

Expected: Platforms section shows 6 platform rows. Enabling one expands it to show tier/print profiles. Enabling Patreon shows the FreePost checkbox and (when unchecked) the Access Tier ID field.

- [ ] **Step 3: Commit**

```bash
git add src/ModelPublisher.ManifestEditor/Views/PlatformsView.axaml
git commit -m "feat: implement PlatformsView with toggle and expand

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 13: AvaloniaFileDialogService + Close Confirmation

**Files:**
- Modify: `src/ModelPublisher.ManifestEditor/Services/AvaloniaFileDialogService.cs`
- Modify: `src/ModelPublisher.ManifestEditor/Views/MainWindow.axaml.cs`

- [ ] **Step 1: Implement AvaloniaFileDialogService.cs**

```csharp
// src/ModelPublisher.ManifestEditor/Services/AvaloniaFileDialogService.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace ModelPublisher.ManifestEditor.Services;

public class AvaloniaFileDialogService : IFileDialogService
{
    private readonly Window _owner;

    public AvaloniaFileDialogService(Window owner) => _owner = owner;

    public async Task<string?> OpenManifestLocationAsync()
    {
        // Try folder first via FolderPicker; fall back to file picker for manifest.json
        var folderResult = await _owner.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = "Select model folder or manifest.json" });

        if (folderResult.Count > 0)
            return folderResult[0].Path.LocalPath;

        // User may have cancelled -- also offer file picker
        return null;
    }

    // Separate method to pick a manifest.json file directly
    public async Task<string?> OpenManifestFileAsync()
    {
        var files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open manifest.json",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Manifest") { Patterns = ["manifest.json"] },
                new FilePickerFileType("JSON") { Patterns = ["*.json"] },
            ]
        });
        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }

    public async Task<IReadOnlyList<string>> OpenFilesAsync(string title, params string[] extensions)
    {
        var patterns = extensions.Select(e => e.StartsWith('.') ? $"*{e}" : e).ToArray();
        var files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("Files") { Patterns = patterns },
            ]
        });
        return files.Select(f => f.Path.LocalPath).ToArray();
    }

    public async Task ShowErrorAsync(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400, Height = 160,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 16,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    new Button { Content = "OK", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right }
                }
            }
        };
        ((Button)((StackPanel)dialog.Content!).Children[1]).Click += (_, _) => dialog.Close();
        await dialog.ShowDialog(_owner);
    }

    public async Task<bool> ConfirmUnsavedChangesAsync()
    {
        var result = false;
        var dialog = new Window
        {
            Title = "Unsaved changes",
            Width = 360, Height = 150,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 16,
                Children =
                {
                    new TextBlock { Text = "You have unsaved changes. Save before opening?" },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        Spacing = 8,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Children =
                        {
                            new Button { Content = "Don't Save" },
                            new Button { Content = "Save" },
                        }
                    }
                }
            }
        };
        var buttons = (StackPanel)((StackPanel)dialog.Content!).Children[1];
        ((Button)buttons.Children[0]).Click += (_, _) => { result = false; dialog.Close(); };
        ((Button)buttons.Children[1]).Click += (_, _) => { result = true; dialog.Close(); };
        await dialog.ShowDialog(_owner);
        return result;
    }

    public async Task ShowValidationErrorsAsync(IReadOnlyList<string> errors)
    {
        var errorText = string.Join("\n", errors.Select(e => $"• {e}"));
        await ShowErrorAsync("Cannot save — validation errors", errorText);
    }
}
```

**Note:** `OpenManifestLocationAsync` on Windows opens a folder picker. However, Avalonia's `StorageProvider` on some platforms may require a combined folder/file approach. If the folder picker doesn't open a file picker when a manifest.json file path is intended, update `MainWindowViewModel.OpenAsync` to use both pickers: try folder, then if the user wants a file, try the file picker. The simplest first-iteration fix: show a dialog asking "Open folder (new/existing) or open manifest.json file?" and branch on the answer. For v1, a folder picker alone is sufficient since the Open flow supports both.

- [ ] **Step 2: Update App.axaml.cs to pass the window to the service**

```csharp
public override void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        var window = new MainWindow();
        window.DataContext = new MainWindowViewModel(new AvaloniaFileDialogService(window));
        desktop.MainWindow = window;
    }
    base.OnFrameworkInitializationCompleted();
}
```

- [ ] **Step 3: Add close confirmation to MainWindow.axaml.cs**

```csharp
// src/ModelPublisher.ManifestEditor/Views/MainWindow.axaml.cs
using Avalonia.Controls;
using Avalonia.Interactivity;
using ModelPublisher.ManifestEditor.ViewModels;

namespace ModelPublisher.ManifestEditor.Views;

public partial class MainWindow : Window
{
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && vm.IsDirty)
        {
            e.Cancel = true;
            var save = await vm.ConfirmAndSaveBeforeCloseAsync();
            if (save) await vm.SaveCommand.ExecuteAsync(null);
            Close(); // re-close without dirty flag
        }
        base.OnClosing(e);
    }
}
```

Add `ConfirmAndSaveBeforeCloseAsync` to `MainWindowViewModel`:

```csharp
public async Task<bool> ConfirmAndSaveBeforeCloseAsync()
    => await _dialogs.ConfirmUnsavedChangesAsync();
```

- [ ] **Step 4: Build and run full integration test**

```bash
dotnet run --project src/ModelPublisher.ManifestEditor/
```

Test manually:
1. Click Open -- folder picker opens
2. Select a folder that has no `manifest.json` -- blank state loads, Save is enabled
3. Fill in a title and click Save -- `manifest.json` is written to the folder
4. Reopen the saved manifest -- all fields are populated correctly
5. Change a field -- title bar shows `*`
6. Click X to close -- "Unsaved changes" prompt appears

- [ ] **Step 5: Commit**

```bash
git add src/ModelPublisher.ManifestEditor/
git commit -m "feat: implement file dialogs and open/save flow

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 14: Run All Tests + Final Commit

- [ ] **Step 1: Run the full test suite**

```bash
cd C:/Source/ModelPublisher
dotnet test tests/ModelPublisher.ManifestEditor.Tests/
```

Expected: all tests pass.

- [ ] **Step 2: Run slopwatch**

```bash
powershell.exe -Command "cd 'C:\Source\ModelPublisher'; slopwatch analyze -d ."
```

Expected: no new issues compared to baseline.

- [ ] **Step 3: Final commit**

```bash
git add -A
git commit -m "feat: complete ManifestEditor v1

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```
