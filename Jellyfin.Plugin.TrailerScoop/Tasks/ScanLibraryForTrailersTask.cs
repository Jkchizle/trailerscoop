using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.TrailerScoop.Services;

namespace Jellyfin.Plugin.TrailerScoop.Tasks
{
    public class ScanLibraryForTrailersTask : IScheduledTask
    {
        private readonly ILibraryManager _lib;
        private readonly ILogger<ScanLibraryForTrailersTask> _log;
        private readonly TrailerManager _manager;

        public ScanLibraryForTrailersTask(
            ILibraryManager lib,
            ILogger<ScanLibraryForTrailersTask> log,
            ILoggerFactory loggerFactory)
        {
            _lib = lib;
            _log = log;
            _manager = new TrailerManager(loggerFactory.CreateLogger<TrailerManager>(), loggerFactory);
        }

        public string Key => "TrailerScoop.Scan";
        public string Name => "TrailerScoop: Scan & fetch trailers";
        public string Description => "Finds movies & series and downloads official trailers (TMDb→YouTube→yt-dlp).";
        public string Category => "Library";

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var cfg = Plugin.Instance!.Configuration;

            var q = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
                Recursive = true
            };

            var items = _lib.GetItemList(q);
            if (items.Count == 0)
            {
                _log.LogInformation("TrailerScoop: no items found.");
                progress?.Report(100);
                return;
            }

            var maxParallel = Math.Max(1, cfg.MaxConcurrentDownloads);
            await using var queue = new DownloadQueue(maxParallel);

            int done = 0;
            var tasks = new List<Task>(items.Count);

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                tasks.Add(queue.RunAsync(async ct =>
                {
                    try
                    {
                        await _manager.EnsureTrailerAsync(item, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        _log.LogWarning(ex, "TrailerScoop: item failed: {Item}", item.Name ?? item.Id.ToString());
                    }
                    finally
                    {
                        var current = Interlocked.Increment(ref done);
                        var pct = (double)current / Math.Max(1, items.Count) * 100.0;
                        progress?.Report(pct);
                    }
                }, cancellationToken));
            }

            await queue.WhenAllAsync(tasks).ConfigureAwait(false);

            _log.LogInformation("TrailerScoop: finished. Processed {Count} items.", items.Count);
            progress?.Report(100);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            yield break; // no default schedule; configure from Jellyfin UI
        }
    }
}
