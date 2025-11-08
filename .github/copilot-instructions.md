## TrailerScoop — Copilot instructions

This file gives an immediate, pragmatic orientation for an AI coding agent working in the TrailerScoop Jellyfin plugin repository.

Scope: the C# Jellyfin plugin in `Jellyfin.Plugin.TrailerScoop/` (targeting .NET 9). Focus on the scheduled scan task, TMDb lookup, and downloads via yt-dlp.

Quick start (build & dev)
- Build the solution from the repo root: `dotnet build TrailerScoop.sln -c Debug`.
- The plugin project output is under `Jellyfin.Plugin.TrailerScoop/bin/Debug/net9.0/`.

Platform-specific deployment paths
- Windows: `%ProgramData%\Jellyfin\Server\plugins` (server-wide) or `%LOCALAPPDATA%\jellyfin\plugins` (user-specific)
- Linux: `/usr/share/jellyfin/plugins` (system) or `~/.local/share/jellyfin/plugins` (user)
- macOS: `/usr/local/share/jellyfin/plugins` (system) or `~/Library/Application Support/jellyfin/plugins` (user)

Quick dev loop (Windows PowerShell)
```powershell
# Set once: your Jellyfin plugins path (edit as needed)
$JF_PLUGINS = "$env:LOCALAPPDATA\jellyfin\plugins"
$PLUGIN_ID = "9e4d3aaa-6d9a-4ad1-9d16-3c9bd6e4e1a9"  # from plugin.json

# One-time: create plugin directory
New-Item -ItemType Directory -Force -Path "$JF_PLUGINS\$PLUGIN_ID"

# Build & copy script (save as deploy-dev.ps1)
dotnet build TrailerScoop.sln -c Debug
if ($LASTEXITCODE -eq 0) {
    Get-Service jellyfin -ErrorAction SilentlyContinue | Stop-Service
    Copy-Item "Jellyfin.Plugin.TrailerScoop\bin\Debug\net9.0\*" "$JF_PLUGINS\$PLUGIN_ID" -Recurse -Force
    Get-Service jellyfin -ErrorAction SilentlyContinue | Start-Service
    Write-Host "Deployed to: $JF_PLUGINS\$PLUGIN_ID"
}
```

After copying: restart Jellyfin, enable plugin in dashboard, check logs in Dashboard > Advanced > Logs.

High-level architecture
- Entry points:
  - `Plugin` in `Jellyfin.Plugin.TrailerScoop/plugin.cs` — plugin identity and configuration holder (`Plugin.Instance`).
  - Scheduled task: `ScanLibraryForTrailersTask` (`Tasks/ScanLibraryForTrailersTask.cs`) implements `IScheduledTask` and scans library items.
- Core services:
  - `TrailerManager` (`Services/TrailerManager.cs`) — coordinates per-item trailer lookup & download (currently a stub). Implement main logic here.
  - `TmdbClient` (`Services/TmdbClient.cs`) — TMDb API integration (requires TMDb API key from `PluginConfiguration.TmdbApiKey`).
  - `YtDlpClient` (`Services/YtDlpClient.cs`) — invokes `yt-dlp` executable; resolves binary via `YtDlpPath` config or PATH.
  - `DownloadQueue` (`Services/DownloadQueue.cs`) — concurrency throttle used by the scan task.

Data & control flow (what to implement/extend)
- `ScanLibraryForTrailersTask.ExecuteAsync` enumerates movies/series and enqueues work on `DownloadQueue`.
- Per item, implement `TrailerManager.EnsureTrailerAsync`: use `TmdbClient.TryGetYoutubeKeyAsync(title, year, lang, region, ct)` to get a YouTube key, then call `YtDlpClient.DownloadAsync(url, outputPathNoExt, maxHeight, preferAvc, ct)` to fetch files. Use `Plugin.Instance.Configuration` for settings (file pattern, max height, etc.).

Project-specific conventions & patterns
- Configuration: `PluginConfiguration` is defined in `plugin.cs` and used via `Plugin.Instance.Configuration` (null-safe; tests should stub `Plugin.Instance`).
- Logging: use constructor-injected `ILogger<T>` / `ILoggerFactory` as shown in services.
- Concurrency: rely on `DownloadQueue` rather than launching unbounded tasks. Respect `MaxConcurrentDownloads` from configuration.
- JSON handling: prefer `JsonDocument` and the small helpers in `Services/JsonElementExt.cs`.
- File naming: default `FilePattern` is `{title}-trailer{lang}-{height}.mp4` — use this template for constructing output filenames.

Integration points & external dependencies
- TMDb: `TmdbClient` calls TMDb REST endpoints and needs `TmdbApiKey` in `PluginConfiguration`.
- yt-dlp: `YtDlpClient` expects an executable available either at `PluginConfiguration.YtDlpPath`, on `PATH`, or in common Windows installs. If missing, `YtDlpClient.DownloadAsync` throws with a clear message.
- Jellyfin server: plugin is a standard Jellyfin plugin (has `plugin.json` with `targetAbi`). Tasks are scheduled via Jellyfin UI; `ScanLibraryForTrailersTask.GetDefaultTriggers()` returns no defaults.

Where to look first for changes
- Implement lookup + download: `Services/TrailerManager.cs` (main place to add logic).
- TMDb details: `Services/TmdbClient.cs` (parsing & selection heuristics live here).
- Binary invocation: `Services/YtDlpClient.cs` (command-line format; error handling for missing binary).
- Concurrency control: `Services/DownloadQueue.cs` (use for throttling downloads).

Small examples & hints
- Example TMDb→YouTube flow: call `TmdbClient.TryGetYoutubeKeyAsync(title, year, lang, region, ct)` → if returns key `k` then use url `https://www.youtube.com/watch?v={k}` for `YtDlpClient.DownloadAsync`.
- To add unit tests, inject `TmdbClient` and `YtDlpClient` behind interfaces (not yet present) or wrap their usage in testable adapters. Current code constructs `YtDlpClient` inside `TrailerManager` — consider making it constructor-injected for testability.

Known gaps & TODOs (observed in code)
- `TrailerManager.EnsureTrailerAsync` is a stub — core feature not implemented yet.
- `GetDefaultTriggers()` yields no schedule — plugin requires manual scheduling in Jellyfin UI to run scans.

Editing & PR tips for the repository
- Keep changes small and implement end-to-end flows (lookup → download → local file creation) rather than only partial logic. The scheduled task wiring is already present.
- Preserve use of `ILogger` and avoid throwing raw exceptions that would stop the whole scan — task code demonstrates catching per-item exceptions and logging warnings.

If anything here is unclear or you want more detail (examples of implementing `EnsureTrailerAsync`, test scaffolding, or packaging steps for a specific OS), tell me which part and I'll expand or generate the code changes.
