using System;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.ServerSync
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string AuthToken { get; set; }

        public string RemoteServerURL { get; set; }

        public PluginConfiguration()
        {
            RemoteServerURL = string.Empty;
            AuthToken = Guid.NewGuid().ToString();
        }
    }
}
