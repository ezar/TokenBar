# TokenBar

TokenBar is a small .NET/Avalonia desktop app for checking AI product quotas and API usage. It currently targets Windows, while keeping the core provider model portable for future Linux and macOS support.

The first supported providers are:

- Codex product usage through Codex OAuth credentials.
- Claude product usage through Claude Code OAuth credentials.
- GitHub Copilot usage through the current Copilot API integration.
- Optional OpenAI API usage through the OpenAI Usage API.
- Optional Anthropic API usage through the Anthropic Admin Usage API.

## Project Structure

```text
src/
  TokenBar.App   Avalonia desktop app with system tray support
  TokenBar.Cli   Command-line usage/status tool
  TokenBar.Core  Configuration, provider APIs, refresh pipeline
tests/
  TokenBar.Core.Tests
```

## Requirements

- .NET SDK 10.0 or newer.
- Windows for the current desktop target.
- API credentials for the providers you want to enable.

## Configure API Keys

The recommended local setup is a root-level file named `appsettings.local.json`:

```json
{
  "TokenBar": {
    "ApiKeys": {
      "CodexAccessToken": "codex-oauth-access-token",
      "CodexAccountId": "optional-account-id",
      "ClaudeCodeOAuthToken": "sk-ant-oat01-...",
      "OpenAIAdminKey": "sk-admin-...",
      "AnthropicAdminKey": "sk-ant-admin...",
      "GitHubCopilotToken": "..."
    }
  }
}
```

`appsettings.local.json` is ignored by git. Do not commit real keys.

You can copy the example file first:

```powershell
Copy-Item appsettings.example.json appsettings.local.json
```

Environment variables are also supported and take priority over `appsettings.local.json`:

```powershell
[Environment]::SetEnvironmentVariable("OPENAI_ADMIN_KEY", "sk-admin-...", "User")
[Environment]::SetEnvironmentVariable("ANTHROPIC_ADMIN_KEY", "sk-ant-admin...", "User")
[Environment]::SetEnvironmentVariable("CLAUDE_CODE_OAUTH_TOKEN", "sk-ant-oat01-...", "User")
[Environment]::SetEnvironmentVariable("CODEX_ACCESS_TOKEN", "...", "User")
[Environment]::SetEnvironmentVariable("CODEX_ACCOUNT_ID", "optional-account-id", "User")
[Environment]::SetEnvironmentVariable("GITHUB_COPILOT_TOKEN", "...", "User")
```

After setting user environment variables, restart Visual Studio, terminals, and TokenBar so new processes can read them.

## Getting The Keys

### Codex

For Codex product limits such as `5h`, `Weekly`, and credits, TokenBar reads OAuth credentials from the Codex CLI auth file:

```text
%USERPROFILE%\.codex\auth.json
```

You can also set `CODEX_ACCESS_TOKEN` and optionally `CODEX_ACCOUNT_ID`, but using the CLI auth file is preferred.

### Claude

For Claude product limits like `Current session` and `Weekly limits`, TokenBar reads OAuth credentials from Claude Code:

```text
%USERPROFILE%\.claude\.credentials.json
```

You can also set `CLAUDE_CODE_OAUTH_TOKEN`. This is different from an Anthropic Admin API key.

### OpenAI API

OpenAI API usage is separate from Codex product quota. To enable the optional `OpenAI API` provider, use an OpenAI Admin API key from the organization settings:

```text
https://platform.openai.com/settings/organization/admin-keys
```

A normal project API key may return `403 Forbidden` for organization usage endpoints. TokenBar prefers `OPENAI_ADMIN_KEY`, then falls back to `OPENAI_API_KEY`.

### Anthropic API

Anthropic API usage is separate from Claude product quota. To enable the optional `Anthropic API` provider, use an Anthropic Admin API key. It is different from a normal Anthropic API key and normally starts with:

```text
sk-ant-admin...
```

TokenBar reads it from `ANTHROPIC_ADMIN_KEY` or `AnthropicAdminKey`.

### GitHub Copilot

TokenBar currently reads `GITHUB_COPILOT_TOKEN`, then `GITHUB_TOKEN`, then `GitHubCopilotToken`.

The Copilot provider is still the least final part of the integration. A future version should move to a first-class OAuth/device login flow so the app can obtain and refresh the token itself.

## Run The App

From the repository root:

```powershell
dotnet run --project src\TokenBar.App\TokenBar.App.csproj
```

Or set `TokenBar.App` as the startup project in Visual Studio.

The app creates a system tray icon. Closing the window hides it to the tray; use the tray menu to show it again, refresh now, or exit.

## Run The CLI

```powershell
dotnet run --project src\TokenBar.Cli\TokenBar.Cli.csproj -- status
dotnet run --project src\TokenBar.Cli\TokenBar.Cli.csproj -- usage
```

`usage` prints one line per provider with status, source, primary usage window, secondary usage window, and any provider message.

## Build And Test

```powershell
dotnet restore TokenBar.sln
dotnet build TokenBar.sln
dotnet test TokenBar.sln
dotnet format TokenBar.sln --verify-no-changes
```

## Security Notes

- Never commit `appsettings.local.json`.
- Never put real API keys in `appsettings.example.json`.
- Prefer environment variables or a local ignored settings file for development.
- Rotate any key that was accidentally committed or shared.

## Current Limitations

- Codex and Claude product quotas are currently OAuth-first.
- CLI fallback for Codex and Claude is planned but not implemented yet.
- Optional OpenAI API usage requires an organization/admin-capable key.
- Optional Anthropic API usage requires an Admin API key.
- Copilot token acquisition is manual for now.
- Linux and macOS are future targets; the current desktop behavior is verified on Windows.
