using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.TrailerScoop
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string PreferredLanguage { get; set; } = "en";
        public string Region { get; set; } = "US";
        public int MaxConcurrentDownloads { get; set; } = 2;
        public int MaxHeight { get; set; } = 1080;
        public bool PreferAvc { get; set; } = true;
        public string TmdbApiKey { get; set; } = "";
        public string YtDlpPath { get; set; } = "";
        public string FilePattern { get; set; } = "{title}-trailer{lang}-{height}.mp4";
    }

    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public static Plugin? Instance { get; private set; }

        public override string Name => "TrailerScoop";
        public override Guid Id => new Guid("9e4d3aaa-6d9a-4ad1-9d16-3c9bd6e4e1a9");

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            yield break; // (Optional UI later)
        }
    }
}
