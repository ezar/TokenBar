# TokenBar Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create the .NET solution foundation for TokenBar: core contracts, provider registry, configuration, refresh orchestration, and tests.

**Architecture:** This phase builds only `TokenBar.Core` and `TokenBar.Core.Tests`. UI, CLI, concrete Codex/Claude providers, and packaging are intentionally deferred until the core abstractions are verified. Provider behavior is driven by descriptors and fetch strategies so later providers can be added without changing shared orchestration.

**Tech Stack:** .NET 10 SDK, C# 14 where available, xUnit, FluentAssertions, `System.Text.Json`.

---

## Prerequisites

The current environment does not have `dotnet` or `git` available in Bash or PowerShell. Install the .NET SDK before executing this plan. Git is recommended for commits, but not required to create the code.

Verify:

```bash
dotnet --info
git --version
```

Expected:

```text
.NET SDK:
 Version: 10.x
git version 2.x
```

If `git` is unavailable, skip commit steps and keep the file changes unstaged.

## File Structure

Create:

```text
TokenBar.sln
Directory.Build.props
src/TokenBar.Core/TokenBar.Core.csproj
src/TokenBar.Core/Providers/ProviderId.cs
src/TokenBar.Core/Providers/ProviderSourceMode.cs
src/TokenBar.Core/Providers/ProviderDescriptor.cs
src/TokenBar.Core/Providers/IUsageProvider.cs
src/TokenBar.Core/Providers/IUsageFetchStrategy.cs
src/TokenBar.Core/Providers/ProviderRegistry.cs
src/TokenBar.Core/Usage/UsageSnapshot.cs
src/TokenBar.Core/Usage/UsageWindow.cs
src/TokenBar.Core/Usage/UsageStatus.cs
src/TokenBar.Core/Refresh/RefreshResult.cs
src/TokenBar.Core/Refresh/RefreshService.cs
src/TokenBar.Core/Configuration/TokenBarConfig.cs
src/TokenBar.Core/Configuration/ProviderConfig.cs
src/TokenBar.Core/Configuration/IConfigStore.cs
src/TokenBar.Core/Configuration/JsonConfigStore.cs
src/TokenBar.Core/Platform/IAppDataPathProvider.cs
src/TokenBar.Core/Platform/IProcessRunner.cs
src/TokenBar.Core/Platform/ProcessRunRequest.cs
src/TokenBar.Core/Platform/ProcessRunResult.cs
src/TokenBar.Core/Platform/SystemProcessRunner.cs
src/TokenBar.Core/Privacy/Redactor.cs
tests/TokenBar.Core.Tests/TokenBar.Core.Tests.csproj
tests/TokenBar.Core.Tests/Usage/UsageWindowTests.cs
tests/TokenBar.Core.Tests/Providers/ProviderRegistryTests.cs
tests/TokenBar.Core.Tests/Refresh/RefreshServiceTests.cs
tests/TokenBar.Core.Tests/Configuration/JsonConfigStoreTests.cs
tests/TokenBar.Core.Tests/Platform/SystemProcessRunnerTests.cs
tests/TokenBar.Core.Tests/Privacy/RedactorTests.cs
```

## Task 1: Scaffold Solution

**Files:**
- Create: `TokenBar.sln`
- Create: `Directory.Build.props`
- Create: `src/TokenBar.Core/TokenBar.Core.csproj`
- Create: `tests/TokenBar.Core.Tests/TokenBar.Core.Tests.csproj`

- [ ] **Step 1: Create solution and projects**

Run:

```bash
dotnet new sln -n TokenBar
mkdir -p src tests
dotnet new classlib -n TokenBar.Core -o src/TokenBar.Core --framework net10.0
dotnet new xunit -n TokenBar.Core.Tests -o tests/TokenBar.Core.Tests --framework net10.0
dotnet sln TokenBar.sln add src/TokenBar.Core/TokenBar.Core.csproj
dotnet sln TokenBar.sln add tests/TokenBar.Core.Tests/TokenBar.Core.Tests.csproj
dotnet add tests/TokenBar.Core.Tests/TokenBar.Core.Tests.csproj reference src/TokenBar.Core/TokenBar.Core.csproj
dotnet add tests/TokenBar.Core.Tests/TokenBar.Core.Tests.csproj package FluentAssertions
rm -f src/TokenBar.Core/Class1.cs tests/TokenBar.Core.Tests/UnitTest1.cs
```

Expected: solution and projects are created, with `TokenBar.Core.Tests` referencing `TokenBar.Core`.

- [ ] **Step 2: Add common build settings**

Create `Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest</AnalysisLevel>
  </PropertyGroup>
</Project>
```

- [ ] **Step 3: Build empty solution**

Run:

```bash
dotnet build TokenBar.sln
```

Expected: `Build succeeded`.

- [ ] **Step 4: Commit**

Run:

```bash
git add TokenBar.sln Directory.Build.props src tests
git commit -m "chore: scaffold tokenbar core solution"
```

Expected: commit succeeds. If `git` is unavailable, record this as skipped.

## Task 2: Usage Domain Models

**Files:**
- Create: `tests/TokenBar.Core.Tests/Usage/UsageWindowTests.cs`
- Create: `src/TokenBar.Core/Usage/UsageStatus.cs`
- Create: `src/TokenBar.Core/Usage/UsageWindow.cs`
- Create: `src/TokenBar.Core/Usage/UsageSnapshot.cs`

- [ ] **Step 1: Write failing usage window tests**

Create `tests/TokenBar.Core.Tests/Usage/UsageWindowTests.cs`:

```csharp
using FluentAssertions;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Tests.Usage;

public sealed class UsageWindowTests
{
    [Fact]
    public void FromUsedAndLimitCalculatesPercentUsed()
    {
        var resetAt = new DateTimeOffset(2026, 5, 19, 12, 0, 0, TimeSpan.Zero);

        var window = UsageWindow.FromUsedAndLimit("Session", 750, 1000, resetAt);

        window.Label.Should().Be("Session");
        window.Used.Should().Be(750);
        window.Limit.Should().Be(1000);
        window.PercentUsed.Should().Be(75);
        window.PercentRemaining.Should().Be(25);
        window.ResetAt.Should().Be(resetAt);
    }

    [Fact]
    public void UnknownWindowHasNoPercentages()
    {
        var window = UsageWindow.Unknown("Weekly");

        window.Label.Should().Be("Weekly");
        window.Used.Should().BeNull();
        window.Limit.Should().BeNull();
        window.PercentUsed.Should().BeNull();
        window.PercentRemaining.Should().BeNull();
        window.ResetAt.Should().BeNull();
    }
}
```

- [ ] **Step 2: Run tests to verify failure**

Run:

```bash
dotnet test tests/TokenBar.Core.Tests/TokenBar.Core.Tests.csproj --filter UsageWindowTests
```

Expected: fails because `UsageWindow` does not exist.

- [ ] **Step 3: Implement usage models**

Create `src/TokenBar.Core/Usage/UsageStatus.cs`:

```csharp
namespace TokenBar.Core.Usage;

public enum UsageStatus
{
    Unknown = 0,
    Available = 1,
    Stale = 2,
    Error = 3,
    Disabled = 4
}
```

Create `src/TokenBar.Core/Usage/UsageWindow.cs`:

```csharp
namespace TokenBar.Core.Usage;

public sealed record UsageWindow(
    string Label,
    decimal? Used,
    decimal? Limit,
    decimal? PercentUsed,
    decimal? PercentRemaining,
    DateTimeOffset? ResetAt)
{
    public static UsageWindow FromUsedAndLimit(
        string label,
        decimal used,
        decimal limit,
        DateTimeOffset? resetAt)
    {
        if (limit <= 0)
        {
            return new UsageWindow(label, used, limit, null, null, resetAt);
        }

        var percentUsed = Math.Round((used / limit) * 100m, 2, MidpointRounding.AwayFromZero);
        var percentRemaining = Math.Max(0m, Math.Round(100m - percentUsed, 2, MidpointRounding.AwayFromZero));

        return new UsageWindow(label, used, limit, percentUsed, percentRemaining, resetAt);
    }

    public static UsageWindow Unknown(string label)
    {
        return new UsageWindow(label, null, null, null, null, null);
    }
}
```

Create `src/TokenBar.Core/Usage/UsageSnapshot.cs`:

```csharp
namespace TokenBar.Core.Usage;

public sealed record UsageSnapshot(
    string ProviderId,
    UsageWindow PrimaryWindow,
    UsageWindow? SecondaryWindow,
    string Source,
    UsageStatus Status,
    DateTimeOffset UpdatedAt,
    string? Identity = null,
    string? Message = null);
```

- [ ] **Step 4: Run tests to verify pass**

Run:

```bash
dotnet test tests/TokenBar.Core.Tests/TokenBar.Core.Tests.csproj --filter UsageWindowTests
```

Expected: `Passed`.

- [ ] **Step 5: Commit**

Run:

```bash
git add src/TokenBar.Core/Usage tests/TokenBar.Core.Tests/Usage
git commit -m "feat: add usage domain models"
```

## Task 3: Provider Registry

**Files:**
- Create: `tests/TokenBar.Core.Tests/Providers/ProviderRegistryTests.cs`
- Create: `src/TokenBar.Core/Providers/ProviderId.cs`
- Create: `src/TokenBar.Core/Providers/ProviderSourceMode.cs`
- Create: `src/TokenBar.Core/Providers/ProviderDescriptor.cs`
- Create: `src/TokenBar.Core/Providers/IUsageProvider.cs`
- Create: `src/TokenBar.Core/Providers/IUsageFetchStrategy.cs`
- Create: `src/TokenBar.Core/Providers/ProviderRegistry.cs`

- [ ] **Step 1: Write failing provider registry tests**

Create `tests/TokenBar.Core.Tests/Providers/ProviderRegistryTests.cs`:

```csharp
using FluentAssertions;
using TokenBar.Core.Providers;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Tests.Providers;

public sealed class ProviderRegistryTests
{
    [Fact]
    public void ConstructorRejectsDuplicateProviderIds()
    {
        var descriptor = new ProviderDescriptor(
            ProviderId: "codex",
            DisplayName: "Codex",
            BrandColor: "#111111",
            DefaultEnabled: true,
            SupportedSourceModes: [ProviderSourceMode.Auto]);

        var providers = new IUsageProvider[]
        {
            new StubProvider(descriptor),
            new StubProvider(descriptor)
        };

        var action = () => new ProviderRegistry(providers);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Duplicate provider id 'codex'*");
    }

    [Fact]
    public void GetEnabledProvidersReturnsConfigOrder()
    {
        var codex = new StubProvider(new ProviderDescriptor("codex", "Codex", "#111111", true, [ProviderSourceMode.Auto]));
        var claude = new StubProvider(new ProviderDescriptor("claude", "Claude", "#DA7756", true, [ProviderSourceMode.Auto]));
        var registry = new ProviderRegistry([codex, claude]);

        var enabled = registry.GetEnabledProviders(["claude", "codex"]);

        enabled.Select(provider => provider.Descriptor.ProviderId)
            .Should().Equal("claude", "codex");
    }

    private sealed class StubProvider(ProviderDescriptor descriptor) : IUsageProvider
    {
        public ProviderDescriptor Descriptor { get; } = descriptor;

        public Task<UsageSnapshot> FetchAsync(ProviderSourceMode sourceMode, CancellationToken cancellationToken)
        {
            return Task.FromResult(new UsageSnapshot(
                Descriptor.ProviderId,
                UsageWindow.Unknown("Session"),
                null,
                "stub",
                UsageStatus.Unknown,
                DateTimeOffset.UnixEpoch));
        }
    }
}
```

- [ ] **Step 2: Run tests to verify failure**

Run:

```bash
dotnet test tests/TokenBar.Core.Tests/TokenBar.Core.Tests.csproj --filter ProviderRegistryTests
```

Expected: fails because provider types do not exist.

- [ ] **Step 3: Implement provider abstractions**

Create `src/TokenBar.Core/Providers/ProviderId.cs`:

```csharp
namespace TokenBar.Core.Providers;

public static class ProviderId
{
    public const string Codex = "codex";
    public const string Claude = "claude";
}
```

Create `src/TokenBar.Core/Providers/ProviderSourceMode.cs`:

```csharp
namespace TokenBar.Core.Providers;

public enum ProviderSourceMode
{
    Auto = 0,
    Cli = 1,
    Logs = 2
}
```

Create `src/TokenBar.Core/Providers/ProviderDescriptor.cs`:

```csharp
namespace TokenBar.Core.Providers;

public sealed record ProviderDescriptor(
    string ProviderId,
    string DisplayName,
    string BrandColor,
    bool DefaultEnabled,
    IReadOnlyList<ProviderSourceMode> SupportedSourceModes);
```

Create `src/TokenBar.Core/Providers/IUsageFetchStrategy.cs`:

```csharp
using TokenBar.Core.Usage;

namespace TokenBar.Core.Providers;

public interface IUsageFetchStrategy
{
    string SourceName { get; }

    ProviderSourceMode SourceMode { get; }

    Task<bool> IsAvailableAsync(CancellationToken cancellationToken);

    Task<UsageSnapshot> FetchAsync(CancellationToken cancellationToken);
}
```

Create `src/TokenBar.Core/Providers/IUsageProvider.cs`:

```csharp
using TokenBar.Core.Usage;

namespace TokenBar.Core.Providers;

public interface IUsageProvider
{
    ProviderDescriptor Descriptor { get; }

    Task<UsageSnapshot> FetchAsync(ProviderSourceMode sourceMode, CancellationToken cancellationToken);
}
```

Create `src/TokenBar.Core/Providers/ProviderRegistry.cs`:

```csharp
namespace TokenBar.Core.Providers;

public sealed class ProviderRegistry
{
    private readonly IReadOnlyDictionary<string, IUsageProvider> providersById;

    public ProviderRegistry(IEnumerable<IUsageProvider> providers)
    {
        var providerList = providers.ToList();
        var duplicate = providerList
            .GroupBy(provider => provider.Descriptor.ProviderId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Duplicate provider id '{duplicate.Key}'.");
        }

        providersById = providerList.ToDictionary(
            provider => provider.Descriptor.ProviderId,
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<IUsageProvider> GetEnabledProviders(IReadOnlyList<string> enabledProviderIds)
    {
        return enabledProviderIds
            .Where(providersById.ContainsKey)
            .Select(providerId => providersById[providerId])
            .ToList();
    }
}
```

- [ ] **Step 4: Run tests to verify pass**

Run:

```bash
dotnet test tests/TokenBar.Core.Tests/TokenBar.Core.Tests.csproj --filter ProviderRegistryTests
```

Expected: `Passed`.

- [ ] **Step 5: Commit**

Run:

```bash
git add src/TokenBar.Core/Providers tests/TokenBar.Core.Tests/Providers
git commit -m "feat: add provider registry abstractions"
```

## Task 4: JSON Configuration Store

**Files:**
- Create: `tests/TokenBar.Core.Tests/Configuration/JsonConfigStoreTests.cs`
- Create: `src/TokenBar.Core/Configuration/TokenBarConfig.cs`
- Create: `src/TokenBar.Core/Configuration/ProviderConfig.cs`
- Create: `src/TokenBar.Core/Configuration/IConfigStore.cs`
- Create: `src/TokenBar.Core/Configuration/JsonConfigStore.cs`
- Create: `src/TokenBar.Core/Platform/IAppDataPathProvider.cs`

- [ ] **Step 1: Write failing config tests**

Create `tests/TokenBar.Core.Tests/Configuration/JsonConfigStoreTests.cs`:

```csharp
using FluentAssertions;
using TokenBar.Core.Configuration;
using TokenBar.Core.Platform;
using TokenBar.Core.Providers;

namespace TokenBar.Core.Tests.Configuration;

public sealed class JsonConfigStoreTests
{
    [Fact]
    public async Task LoadAsyncReturnsDefaultConfigWhenFileDoesNotExist()
    {
        using var temp = new TempDirectory();
        var store = new JsonConfigStore(new StubPathProvider(temp.Path));

        var config = await store.LoadAsync(CancellationToken.None);

        config.RefreshInterval.Should().Be(TimeSpan.FromMinutes(5));
        config.Providers.Select(provider => provider.ProviderId)
            .Should().Equal(ProviderId.Codex, ProviderId.Claude);
    }

    [Fact]
    public async Task SaveAsyncPersistsConfig()
    {
        using var temp = new TempDirectory();
        var store = new JsonConfigStore(new StubPathProvider(temp.Path));
        var config = TokenBarConfig.CreateDefault() with
        {
            RefreshInterval = TimeSpan.FromMinutes(1)
        };

        await store.SaveAsync(config, CancellationToken.None);
        var loaded = await store.LoadAsync(CancellationToken.None);

        loaded.RefreshInterval.Should().Be(TimeSpan.FromMinutes(1));
    }

    private sealed class StubPathProvider(string path) : IAppDataPathProvider
    {
        public string AppDataDirectory => path;
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
```

- [ ] **Step 2: Run tests to verify failure**

Run:

```bash
dotnet test tests/TokenBar.Core.Tests/TokenBar.Core.Tests.csproj --filter JsonConfigStoreTests
```

Expected: fails because config types do not exist.

- [ ] **Step 3: Implement config store**

Create `src/TokenBar.Core/Configuration/ProviderConfig.cs`:

```csharp
using TokenBar.Core.Providers;

namespace TokenBar.Core.Configuration;

public sealed record ProviderConfig(
    string ProviderId,
    bool Enabled,
    ProviderSourceMode SourceMode);
```

Create `src/TokenBar.Core/Configuration/TokenBarConfig.cs`:

```csharp
using TokenBar.Core.Providers;

namespace TokenBar.Core.Configuration;

public sealed record TokenBarConfig(
    TimeSpan RefreshInterval,
    IReadOnlyList<ProviderConfig> Providers)
{
    public static TokenBarConfig CreateDefault()
    {
        return new TokenBarConfig(
            TimeSpan.FromMinutes(5),
            [
                new ProviderConfig(ProviderId.Codex, true, ProviderSourceMode.Auto),
                new ProviderConfig(ProviderId.Claude, true, ProviderSourceMode.Auto)
            ]);
    }
}
```

Create `src/TokenBar.Core/Configuration/IConfigStore.cs`:

```csharp
namespace TokenBar.Core.Configuration;

public interface IConfigStore
{
    Task<TokenBarConfig> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(TokenBarConfig config, CancellationToken cancellationToken);
}
```

Create `src/TokenBar.Core/Platform/IAppDataPathProvider.cs`:

```csharp
namespace TokenBar.Core.Platform;

public interface IAppDataPathProvider
{
    string AppDataDirectory { get; }
}
```

Create `src/TokenBar.Core/Configuration/JsonConfigStore.cs`:

```csharp
using System.Text.Json;
using TokenBar.Core.Platform;

namespace TokenBar.Core.Configuration;

public sealed class JsonConfigStore(IAppDataPathProvider pathProvider) : IConfigStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private string ConfigPath => Path.Combine(pathProvider.AppDataDirectory, "config.json");

    public async Task<TokenBarConfig> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(ConfigPath))
        {
            return TokenBarConfig.CreateDefault();
        }

        await using var stream = File.OpenRead(ConfigPath);
        var config = await JsonSerializer.DeserializeAsync<TokenBarConfig>(
            stream,
            SerializerOptions,
            cancellationToken);

        return config ?? TokenBarConfig.CreateDefault();
    }

    public async Task SaveAsync(TokenBarConfig config, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(pathProvider.AppDataDirectory);
        await using var stream = File.Create(ConfigPath);
        await JsonSerializer.SerializeAsync(stream, config, SerializerOptions, cancellationToken);
    }
}
```

- [ ] **Step 4: Run tests to verify pass**

Run:

```bash
dotnet test tests/TokenBar.Core.Tests/TokenBar.Core.Tests.csproj --filter JsonConfigStoreTests
```

Expected: `Passed`.

- [ ] **Step 5: Commit**

Run:

```bash
git add src/TokenBar.Core/Configuration src/TokenBar.Core/Platform/IAppDataPathProvider.cs tests/TokenBar.Core.Tests/Configuration
git commit -m "feat: add json configuration store"
```

## Task 5: Process Runner

**Files:**
- Create: `tests/TokenBar.Core.Tests/Platform/SystemProcessRunnerTests.cs`
- Create: `src/TokenBar.Core/Platform/IProcessRunner.cs`
- Create: `src/TokenBar.Core/Platform/ProcessRunRequest.cs`
- Create: `src/TokenBar.Core/Platform/ProcessRunResult.cs`
- Create: `src/TokenBar.Core/Platform/SystemProcessRunner.cs`

- [ ] **Step 1: Write failing process runner tests**

Create `tests/TokenBar.Core.Tests/Platform/SystemProcessRunnerTests.cs`:

```csharp
using FluentAssertions;
using TokenBar.Core.Platform;

namespace TokenBar.Core.Tests.Platform;

public sealed class SystemProcessRunnerTests
{
    [Fact]
    public async Task RunAsyncCapturesStdout()
    {
        var runner = new SystemProcessRunner();
        var request = OperatingSystem.IsWindows()
            ? new ProcessRunRequest("cmd.exe", ["/c", "echo tokenbar"], TimeSpan.FromSeconds(5))
            : new ProcessRunRequest("/bin/sh", ["-c", "printf tokenbar"], TimeSpan.FromSeconds(5));

        var result = await runner.RunAsync(request, CancellationToken.None);

        result.ExitCode.Should().Be(0);
        result.StandardOutput.Trim().Should().Be("tokenbar");
        result.TimedOut.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsyncMarksTimeout()
    {
        var runner = new SystemProcessRunner();
        var request = OperatingSystem.IsWindows()
            ? new ProcessRunRequest("cmd.exe", ["/c", "ping -n 3 127.0.0.1 > nul"], TimeSpan.FromMilliseconds(50))
            : new ProcessRunRequest("/bin/sh", ["-c", "sleep 2"], TimeSpan.FromMilliseconds(50));

        var result = await runner.RunAsync(request, CancellationToken.None);

        result.TimedOut.Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run tests to verify failure**

Run:

```bash
dotnet test tests/TokenBar.Core.Tests/TokenBar.Core.Tests.csproj --filter SystemProcessRunnerTests
```

Expected: fails because process runner types do not exist.

- [ ] **Step 3: Implement process runner**

Create `src/TokenBar.Core/Platform/ProcessRunRequest.cs`:

```csharp
namespace TokenBar.Core.Platform;

public sealed record ProcessRunRequest(
    string FileName,
    IReadOnlyList<string> Arguments,
    TimeSpan Timeout,
    string? WorkingDirectory = null);
```

Create `src/TokenBar.Core/Platform/ProcessRunResult.cs`:

```csharp
namespace TokenBar.Core.Platform;

public sealed record ProcessRunResult(
    int? ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut);
```

Create `src/TokenBar.Core/Platform/IProcessRunner.cs`:

```csharp
namespace TokenBar.Core.Platform;

public interface IProcessRunner
{
    Task<ProcessRunResult> RunAsync(ProcessRunRequest request, CancellationToken cancellationToken);
}
```

Create `src/TokenBar.Core/Platform/SystemProcessRunner.cs`:

```csharp
using System.Diagnostics;

namespace TokenBar.Core.Platform;

public sealed class SystemProcessRunner : IProcessRunner
{
    public async Task<ProcessRunResult> RunAsync(ProcessRunRequest request, CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(request.Timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        using var process = new Process();
        process.StartInfo.FileName = request.FileName;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        if (!string.IsNullOrWhiteSpace(request.WorkingDirectory))
        {
            process.StartInfo.WorkingDirectory = request.WorkingDirectory;
        }

        foreach (var argument in request.Arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();
        var stdoutTask = process.StandardOutput.ReadToEndAsync(linked.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(linked.Token);

        try
        {
            await process.WaitForExitAsync(linked.Token);
            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            return new ProcessRunResult(process.ExitCode, stdout, stderr, TimedOut: false);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            return new ProcessRunResult(null, string.Empty, string.Empty, TimedOut: true);
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }
}
```

- [ ] **Step 4: Run tests to verify pass**

Run:

```bash
dotnet test tests/TokenBar.Core.Tests/TokenBar.Core.Tests.csproj --filter SystemProcessRunnerTests
```

Expected: `Passed`.

- [ ] **Step 5: Commit**

Run:

```bash
git add src/TokenBar.Core/Platform tests/TokenBar.Core.Tests/Platform
git commit -m "feat: add process runner abstraction"
```

## Task 6: Refresh Service

**Files:**
- Create: `tests/TokenBar.Core.Tests/Refresh/RefreshServiceTests.cs`
- Create: `src/TokenBar.Core/Refresh/RefreshResult.cs`
- Create: `src/TokenBar.Core/Refresh/RefreshService.cs`

- [ ] **Step 1: Write failing refresh tests**

Create `tests/TokenBar.Core.Tests/Refresh/RefreshServiceTests.cs`:

```csharp
using FluentAssertions;
using TokenBar.Core.Configuration;
using TokenBar.Core.Providers;
using TokenBar.Core.Refresh;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Tests.Refresh;

public sealed class RefreshServiceTests
{
    [Fact]
    public async Task RefreshOnceAsyncFetchesEnabledProvidersInConfigOrder()
    {
        var claude = new StubProvider("claude");
        var codex = new StubProvider("codex");
        var service = new RefreshService(new ProviderRegistry([codex, claude]));
        var config = new TokenBarConfig(
            TimeSpan.FromMinutes(5),
            [
                new ProviderConfig("claude", true, ProviderSourceMode.Logs),
                new ProviderConfig("codex", true, ProviderSourceMode.Cli)
            ]);

        var result = await service.RefreshOnceAsync(config, CancellationToken.None);

        result.Snapshots.Select(snapshot => snapshot.ProviderId)
            .Should().Equal("claude", "codex");
        claude.LastSourceMode.Should().Be(ProviderSourceMode.Logs);
        codex.LastSourceMode.Should().Be(ProviderSourceMode.Cli);
    }

    [Fact]
    public async Task RefreshOnceAsyncReturnsErrorSnapshotWhenProviderFails()
    {
        var provider = new ThrowingProvider("codex");
        var service = new RefreshService(new ProviderRegistry([provider]));
        var config = new TokenBarConfig(
            TimeSpan.FromMinutes(5),
            [new ProviderConfig("codex", true, ProviderSourceMode.Auto)]);

        var result = await service.RefreshOnceAsync(config, CancellationToken.None);

        result.Snapshots.Should().ContainSingle();
        result.Snapshots[0].Status.Should().Be(UsageStatus.Error);
        result.Snapshots[0].Message.Should().Be("provider exploded");
    }

    private sealed class StubProvider(string providerId) : IUsageProvider
    {
        public ProviderDescriptor Descriptor { get; } = new(providerId, providerId, "#000000", true, [ProviderSourceMode.Auto]);

        public ProviderSourceMode? LastSourceMode { get; private set; }

        public Task<UsageSnapshot> FetchAsync(ProviderSourceMode sourceMode, CancellationToken cancellationToken)
        {
            LastSourceMode = sourceMode;
            return Task.FromResult(new UsageSnapshot(
                providerId,
                UsageWindow.Unknown("Session"),
                null,
                sourceMode.ToString(),
                UsageStatus.Available,
                DateTimeOffset.UnixEpoch));
        }
    }

    private sealed class ThrowingProvider(string providerId) : IUsageProvider
    {
        public ProviderDescriptor Descriptor { get; } = new(providerId, providerId, "#000000", true, [ProviderSourceMode.Auto]);

        public Task<UsageSnapshot> FetchAsync(ProviderSourceMode sourceMode, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("provider exploded");
        }
    }
}
```

- [ ] **Step 2: Run tests to verify failure**

Run:

```bash
dotnet test tests/TokenBar.Core.Tests/TokenBar.Core.Tests.csproj --filter RefreshServiceTests
```

Expected: fails because refresh types do not exist.

- [ ] **Step 3: Implement refresh service**

Create `src/TokenBar.Core/Refresh/RefreshResult.cs`:

```csharp
using TokenBar.Core.Usage;

namespace TokenBar.Core.Refresh;

public sealed record RefreshResult(
    IReadOnlyList<UsageSnapshot> Snapshots,
    DateTimeOffset RefreshedAt);
```

Create `src/TokenBar.Core/Refresh/RefreshService.cs`:

```csharp
using TokenBar.Core.Configuration;
using TokenBar.Core.Providers;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Refresh;

public sealed class RefreshService(ProviderRegistry providerRegistry)
{
    public async Task<RefreshResult> RefreshOnceAsync(TokenBarConfig config, CancellationToken cancellationToken)
    {
        var enabledConfig = config.Providers.Where(provider => provider.Enabled).ToList();
        var providers = providerRegistry.GetEnabledProviders(enabledConfig.Select(provider => provider.ProviderId).ToList());
        var snapshots = new List<UsageSnapshot>();

        foreach (var provider in providers)
        {
            var providerConfig = enabledConfig.First(configItem =>
                string.Equals(configItem.ProviderId, provider.Descriptor.ProviderId, StringComparison.OrdinalIgnoreCase));

            try
            {
                snapshots.Add(await provider.FetchAsync(providerConfig.SourceMode, cancellationToken));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                snapshots.Add(new UsageSnapshot(
                    provider.Descriptor.ProviderId,
                    UsageWindow.Unknown("Session"),
                    null,
                    "error",
                    UsageStatus.Error,
                    DateTimeOffset.UtcNow,
                    Message: ex.Message));
            }
        }

        return new RefreshResult(snapshots, DateTimeOffset.UtcNow);
    }
}
```

- [ ] **Step 4: Run tests to verify pass**

Run:

```bash
dotnet test tests/TokenBar.Core.Tests/TokenBar.Core.Tests.csproj --filter RefreshServiceTests
```

Expected: `Passed`.

- [ ] **Step 5: Commit**

Run:

```bash
git add src/TokenBar.Core/Refresh tests/TokenBar.Core.Tests/Refresh
git commit -m "feat: add refresh service"
```

## Task 7: Redaction

**Files:**
- Create: `tests/TokenBar.Core.Tests/Privacy/RedactorTests.cs`
- Create: `src/TokenBar.Core/Privacy/Redactor.cs`

- [ ] **Step 1: Write failing redactor tests**

Create `tests/TokenBar.Core.Tests/Privacy/RedactorTests.cs`:

```csharp
using FluentAssertions;
using TokenBar.Core.Privacy;

namespace TokenBar.Core.Tests.Privacy;

public sealed class RedactorTests
{
    [Fact]
    public void RedactMasksBearerTokensApiKeysAndEmails()
    {
        const string input = "Authorization: Bearer sk-test-123\napi_key=abc123\nuser=a@example.com";

        var output = Redactor.Redact(input);

        output.Should().NotContain("sk-test-123");
        output.Should().NotContain("abc123");
        output.Should().NotContain("a@example.com");
        output.Should().Contain("Authorization: Bearer [redacted]");
        output.Should().Contain("api_key=[redacted]");
        output.Should().Contain("[redacted-email]");
    }
}
```

- [ ] **Step 2: Run tests to verify failure**

Run:

```bash
dotnet test tests/TokenBar.Core.Tests/TokenBar.Core.Tests.csproj --filter RedactorTests
```

Expected: fails because `Redactor` does not exist.

- [ ] **Step 3: Implement redactor**

Create `src/TokenBar.Core/Privacy/Redactor.cs`:

```csharp
using System.Text.RegularExpressions;

namespace TokenBar.Core.Privacy;

public static partial class Redactor
{
    public static string Redact(string input)
    {
        var output = BearerTokenRegex().Replace(input, "Authorization: Bearer [redacted]");
        output = ApiKeyRegex().Replace(output, "$1=[redacted]");
        output = EmailRegex().Replace(output, "[redacted-email]");
        return output;
    }

    [GeneratedRegex("Authorization:\\s*Bearer\\s+[^\\r\\n]+", RegexOptions.IgnoreCase)]
    private static partial Regex BearerTokenRegex();

    [GeneratedRegex("(api[_-]?key)\\s*=\\s*[^\\s&]+", RegexOptions.IgnoreCase)]
    private static partial Regex ApiKeyRegex();

    [GeneratedRegex("[A-Z0-9._%+-]+@[A-Z0-9.-]+\\.[A-Z]{2,}", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();
}
```

- [ ] **Step 4: Run tests to verify pass**

Run:

```bash
dotnet test tests/TokenBar.Core.Tests/TokenBar.Core.Tests.csproj --filter RedactorTests
```

Expected: `Passed`.

- [ ] **Step 5: Commit**

Run:

```bash
git add src/TokenBar.Core/Privacy tests/TokenBar.Core.Tests/Privacy
git commit -m "feat: add diagnostic redaction"
```

## Task 8: Full Verification

**Files:**
- Modify only if verification exposes compile or test failures.

- [ ] **Step 1: Run full test suite**

Run:

```bash
dotnet test TokenBar.sln
```

Expected: all tests pass.

- [ ] **Step 2: Run full build**

Run:

```bash
dotnet build TokenBar.sln
```

Expected: `Build succeeded`.

- [ ] **Step 3: Check formatting**

Run:

```bash
dotnet format TokenBar.sln --verify-no-changes
```

Expected: no formatting changes required. If formatting changes are required, run:

```bash
dotnet format TokenBar.sln
dotnet test TokenBar.sln
```

Expected: formatting completes and tests still pass.

- [ ] **Step 4: Commit verification fixes**

Run:

```bash
git add .
git commit -m "test: verify tokenbar foundation"
```

Expected: commit succeeds if there were verification fixes. If there were no changes, skip this commit.

## Self-Review

Spec coverage:

- Foundation project structure is covered by Task 1.
- Provider contracts and registry are covered by Task 3.
- Usage snapshots and statuses are covered by Task 2.
- Configuration JSON and provider preferences are covered by Task 4.
- Process timeout handling for future CLI providers is covered by Task 5.
- Refresh orchestration and provider error snapshots are covered by Task 6.
- Diagnostic redaction is covered by Task 7.

Deferred by design:

- Concrete Codex/OpenAI provider.
- Concrete Claude/Anthropic provider.
- Avalonia tray app.
- CLI executable.
- DPAPI secret store.
- Installer and packaging.

Those belong in later plans after this foundation passes tests.
