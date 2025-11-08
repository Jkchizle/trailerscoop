# Set Jellyfin plugins path (user-specific install)
$JF_PLUGINS = "$env:LOCALAPPDATA\jellyfin\plugins"
$PLUGIN_ID = "9e4d3aaa-6d9a-4ad1-9d16-3c9bd6e4e1a9"  # from plugin.json

# Create plugin directory
New-Item -ItemType Directory -Force -Path "$JF_PLUGINS\$PLUGIN_ID"

# Build & copy
dotnet build TrailerScoop.sln -c Debug
if ($LASTEXITCODE -eq 0) {
    Get-Service jellyfin -ErrorAction SilentlyContinue | Stop-Service
    Copy-Item "Jellyfin.Plugin.TrailerScoop\bin\Debug\net9.0\*" "$JF_PLUGINS\$PLUGIN_ID" -Recurse -Force
    Get-Service jellyfin -ErrorAction SilentlyContinue | Start-Service
    Write-Host "Deployed to: $JF_PLUGINS\$PLUGIN_ID"
}