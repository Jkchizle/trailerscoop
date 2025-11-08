using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TrailerScoop.Services;

internal sealed class YtDlpClient
{
    private readonly ILogger _log;
    private readonly string _ytDlpPath;

    public YtDlpClient(ILogger log, string? configuredPath)
    {
        _log = log;
        _ytDlpPath = ResolveYtDlp(configuredPath);
    }

    private static string ResolveYtDlp(string? path)
    {
        // Priority: configured -> PATH -> common Windows location
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            return path;

        var envPath = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var p in envPath.Split(Path.PathSeparator))
        {
            var cand = Path.Combine(p, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "yt-dlp.exe" : "yt-dlp");
            if (File.Exists(cand)) return cand;
        }

        // Last ditch: a typical Windows user install
        var userCand = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Local", "Microsoft", "WindowsApps", "yt-dlp.exe");
        if (File.Exists(userCand)) return userCand;

        // If not found, we will error out at call-time with a clear message
        return "yt-dlp";
    }

    public async Task DownloadAsync(string url, string outputPathNoExt, int maxHeight, bool preferAvc, CancellationToken ct)
    {
        var outTemplate = outputPathNoExt + ".%(ext)s";
        var args =
            $"--no-warnings --no-progress -f \"bv*[height<={maxHeight}]{(preferAvc ? "[vcodec~=^((?i)avc|h264))$]" : "")}+ba/best\" " +
            $"--merge-output-format mp4 " +
            $"--output \"{outTemplate}\" " +
            $"\"{url}\"";

        var psi = new ProcessStartInfo
        {
            FileName = _ytDlpPath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        _log.LogInformation("yt-dlp: {Args}", args);

        try
        {
            using var p = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start yt-dlp");
            var stderr = await p.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
            var stdout = await p.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
            await p.WaitForExitAsync(ct).ConfigureAwait(false);

            if (p.ExitCode != 0)
                throw new Exception($"yt-dlp exited with code {p.ExitCode}\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        }
        catch (Win32Exception)
        {
            throw new Exception("yt-dlp not found. Install it and/or set the YtDlpPath in TrailerScoop settings.");
        }
    }
}
