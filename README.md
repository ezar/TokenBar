# TokenBar

TokenBar is a small .NET/Avalonia desktop app for checking AI provider usage from API data. It currently targets Windows, while keeping the core provider model portable for future Linux and macOS support.

The first supported providers are:

- Codex / OpenAI usage through the OpenAI Usage API.
- Claude / Anthropic usage through the Anthropic Admin Usage API.
- GitHub Copilot usage through the current Copilot API integration.

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
[Environment]::SetEnvironmentVariable("GITHUB_COPILOT_TOKEN", "...", "User")
```

After setting user environment variables, restart Visual Studio, terminals, and TokenBar so new processes can read them.

## Getting The Keys

### OpenAI

Use an OpenAI Admin API key from the organization settings:

```text
https://platform.openai.com/settings/organization/admin-keys
```

A normal project API key may return `403 Forbidden` for organization usage endpoints. TokenBar prefers `OPENAI_ADMIN_KEY`, then falls back to `OPENAI_API_KEY`.

### Anthropic

Use an Anthropic Admin API key. It is different from a normal Anthropic API key and normally starts with:

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

- Usage limits are reported from provider APIs, not from local CLI logs.
- OpenAI usage requires an organization/admin-capable key.
- Anthropic usage requires an Admin API key.
- Copilot token acquisition is manual for now.
- Linux and macOS are future targets; the current desktop behavior is verified on Windows.
