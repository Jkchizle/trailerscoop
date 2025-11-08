using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TrailerScoop.Services
{
    public class TrailerManager
    {
        private readonly ILogger<TrailerManager> _log;
        private readonly ILoggerFactory _loggerFactory;
        private readonly PluginConfiguration _cfg;
        private readonly YtDlpClient _yt;

        public TrailerManager(ILogger<TrailerManager> log, ILoggerFactory loggerFactory)
        {
            _log = log;
            _loggerFactory = loggerFactory;
            _cfg = Plugin.Instance?.Configuration ?? new PluginConfiguration();

            var ytLogger = loggerFactory.CreateLogger<YtDlpClient>();
            _yt = new YtDlpClient(ytLogger, _cfg.YtDlpPath);
        }

        public async Task EnsureTrailerAsync(BaseItem item, CancellationToken ct)
        {
            // Only process Movies & Series (you can expand later)
            if (item is not Movie && item is not Series)
            {
                return;
            }

            // Example: read file naming pattern (now exists on PluginConfiguration)
            var pattern = _cfg.FilePattern;

            // TODO: implement actual lookup (TMDb) + dl (yt-dlp) here.
            _log.LogInformation("TrailerScoop: (stub) would fetch trailer for: {Name}", item.Name ?? item.Id.ToString());

            await Task.CompletedTask;
        }
    }
}
