# TokenBar Design

## Context

TokenBar will be a lightweight desktop utility for showing AI coding-tool usage and availability. The first supported providers are Codex/OpenAI and Claude/Anthropic. The initial target platform is Windows, but the architecture must keep Linux and macOS support viable without rewriting the core.

The design is inspired by CodexBar and Win-CodexBar, but it is not a direct port. CodexBar's useful pattern is a provider-driven core where each provider declares its metadata, supported sources, and fetch strategies. Win-CodexBar validates the Windows product shape: tray app, compact usage panel, local-first privacy, CLI support, and Windows credential protection.

## Goals

- Build a Windows desktop tray application in modern .NET.
- Keep the UI technology portable enough for future Linux and macOS builds.
- Start with Codex/OpenAI and Claude/Anthropic.
- Prefer local and CLI-based data sources for the MVP.
- Keep provider code isolated so future providers can be added without cross-provider branching.
- Include a CLI surface for automation and debugging.
- Avoid storing raw secrets unless a future feature explicitly requires it.

## Non-Goals For MVP

- Browser cookie import.
- OAuth login flows inside the app.
- OpenAI/Claude web dashboard scraping.
- Installer, auto-update, and signing.
- Widgets.
- Multi-account switching.
- Full pricing/cost dashboards.

## Platform Choice

TokenBar will use Avalonia UI on .NET. Avalonia is the best fit because it supports Windows now and gives a practical path to Linux and macOS later. WinUI 3 would provide the most native Windows UI, but it would make cross-platform support a future rewrite. .NET MAUI is not selected because Linux desktop support is not a good primary path for this product.

The first development and validation target is Windows 10/11. Linux and macOS support should be protected by keeping platform-specific features behind interfaces.

## Solution Structure

```text
TokenBar.sln
src/
  TokenBar.Core/
  TokenBar.App/
  TokenBar.Cli/
tests/
  TokenBar.Core.Tests/
```

`TokenBar.Core` owns provider contracts, refresh orchestration, configuration, snapshots, parsing, process execution, clock abstractions, and platform service interfaces. It must not reference Avalonia.

`TokenBar.App` owns the Avalonia desktop shell, tray icon, compact panel, settings UI, notifications, and platform-specific composition.

`TokenBar.Cli` exposes the same provider pipeline for commands such as usage, refresh, status, and config.

`TokenBar.Core.Tests` covers parsers, source selection, fallback behavior, refresh timing, config serialization, and redaction.

## Provider Model

Providers are descriptor-driven. A provider descriptor is the single source of truth for identity, display labels, capabilities, default enablement, supported data sources, and source priority.

```text
ProviderDescriptor
  Id
  DisplayName
  BrandColor
  DefaultEnabled
  Capabilities
  SourceModes
  StrategyPipeline
```

Each provider implements one or more `IUsageFetchStrategy` instances. A strategy advertises whether it is available, fetches a usage snapshot, and decides whether fallback is allowed when it fails.

The shared usage result is a `UsageSnapshot`:

```text
UsageSnapshot
  ProviderId
  PrimaryWindow
  SecondaryWindow
  Credits
  Identity
  Source
  Status
  UpdatedAt
```

Usage windows hold percent used or remaining, reset timing, labels, and optional raw limits when known.

## Initial Providers

### Codex/OpenAI

The MVP uses local-first sources:

1. Codex CLI/RPC or CLI diagnostic command when available.
2. Codex local logs under `CODEX_HOME` or `~/.codex`.

The MVP should report a best-effort session and weekly view when the CLI exposes enough data. Local log scanning can initially focus on token/cost summaries rather than exact rate-limit windows if the reset information is not available.

Future Codex work can add OAuth usage, OpenAI web dashboard extras, credits, and richer cost history.

### Claude/Anthropic

The MVP uses local-first sources:

1. Claude CLI diagnostic usage/status when available.
2. Claude local logs under `CLAUDE_CONFIG_DIR`, `~/.config/claude/projects`, or `~/.claude/projects`.

The MVP should report current session and weekly usage when the CLI exposes it. Local log scanning can initially provide token/cost summaries and should not depend on a customizable Claude status line.

Future Claude work can add Anthropic Admin API, OAuth, cookie-based web API, extra usage spend, and multi-account support.

## Source Selection

Each provider supports:

- `Auto`: choose the best available source.
- `Cli`: use provider CLI only.
- `Logs`: use local logs only.

For MVP, `Auto` tries CLI first and then logs. Fallback attempts must be visible in diagnostics so errors are explainable.

## Refresh Loop

The app supports manual refresh and automatic refresh. MVP refresh intervals are:

- Manual
- 1 minute
- 5 minutes
- 15 minutes

The default is 5 minutes.

Refresh runs in the background and updates a shared provider state store. Failures do not clear the last successful snapshot immediately; instead the snapshot becomes stale and the UI shows the latest error. Fetches must have timeouts to avoid a hung CLI blocking refresh.

## UI Design

The MVP UI is compact and work-focused:

- A tray icon starts the app without a main window.
- Clicking the tray icon opens a small panel.
- The panel shows one row per enabled provider.
- Each row shows provider name, source, primary usage, secondary usage when available, reset text, status, and last updated time.
- A refresh button is always available.
- Settings include provider toggles, refresh cadence, source selection, and basic diagnostics.

The tray icon can start as a static app icon plus status indication. Dynamic usage bars can come after the provider pipeline is stable.

## Configuration

Configuration is stored locally as JSON. The initial config contains:

- Enabled providers.
- Provider order.
- Refresh interval.
- Preferred source per provider.
- Basic display settings.

The config file should live in an OS-appropriate application data folder through an abstraction, not hard-coded platform paths.

## Secrets And Privacy

The MVP should avoid requiring stored secrets. If a future feature stores API keys or tokens, secret storage must go through `ISecretStore`.

Initial platform plan:

- Windows: DPAPI-backed store.
- macOS future: Keychain-backed store.
- Linux future: libsecret or a documented fallback.

Diagnostics must redact tokens, cookies, bearer headers, API keys, and email addresses where practical.

## CLI

The CLI reuses `TokenBar.Core` and supports:

```text
tokenbar usage
tokenbar usage --provider codex
tokenbar usage --provider claude
tokenbar status
tokenbar config providers
```

Text output is optimized for humans. JSON output can be added with `--json` early if it is cheap, because it will help testing and future integrations.

## Testing

Core tests are required before broad UI work:

- Provider descriptor registration.
- Source selection and fallback.
- CLI process timeout handling.
- Parser samples for Codex and Claude output.
- Local log scanner fixtures.
- Config read/write.
- Redaction.

UI tests can be lighter in MVP, but manual Windows validation is required for tray behavior before calling the app usable.

## Implementation Phases

### Phase 1: Foundation

Create the solution, projects, core contracts, configuration model, provider registry, refresh loop, and tests.

### Phase 2: Provider MVP

Implement Codex and Claude CLI/log strategies with deterministic parser tests and clear diagnostics.

### Phase 3: Desktop Shell

Build the Avalonia tray app, compact panel, settings, refresh actions, and stale/error states.

### Phase 4: CLI

Add the command-line executable over the same provider pipeline.

### Phase 5: Windows Hardening

Add Windows-specific app data paths, a minimal DPAPI-backed secret store for future credentials, packaging notes, icon polish, and manual validation.

### Future Phases

Add OAuth/API/cookie sources, dynamic tray usage rendering, installer/signing, multi-account support, richer cost dashboards, and Linux/macOS packaging.

## Open Decisions

- Exact CLI command flow for Codex and Claude will be verified during implementation against installed CLIs.
- The first dynamic tray icon design is deferred until usage snapshots are reliable.
- Installer technology is deferred until the app has a stable MVP.

## References

- CodexBar: https://github.com/steipete/CodexBar
- CodexBar provider architecture: https://raw.githubusercontent.com/steipete/CodexBar/main/docs/provider.md
- Codex provider notes: https://raw.githubusercontent.com/steipete/CodexBar/main/docs/codex.md
- Claude provider notes: https://raw.githubusercontent.com/steipete/CodexBar/main/docs/claude.md
- Win-CodexBar: https://github.com/Finesssee/Win-CodexBar
- Avalonia supported platforms: https://docs.avaloniaui.net/docs/supported-platforms
