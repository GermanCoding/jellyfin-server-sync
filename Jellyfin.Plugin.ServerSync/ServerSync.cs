using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Web;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ServerSync
{
    public class ServerSync : IServerEntryPoint
    {
        private static readonly HttpClient client = new HttpClient();

        private readonly IUserManager _userManager;
        private readonly ILogger _logger;
        private readonly IUserDataManager _userDataRepository;

        public ServerSync(IUserManager userManager, ILogger<ServerSync> logger, IUserDataManager userDataRepository)
        {
            _userManager = userManager;
            _logger = logger;
            _userDataRepository = userDataRepository;
        }

        public Task RunAsync()
        {
            _userDataRepository.UserDataSaved += UserDataSaved;
            client.DefaultRequestHeaders.Add("User-Agent", "Jellyfin ServerSync");
            return Task.CompletedTask;
        }

        private void UserDataSaved(object sender, UserDataSaveEventArgs e)
        {
            try
            {
                // Skip non standard reasons, avoid loops when sending this event to remote
                // Skip any invalid events or unconfigured setups
                if (e.SaveReason == UserDataSaveReason.Import || e.Item == null || e.Item.Id == Guid.Empty || string.IsNullOrEmpty(Plugin.Instance.Configuration.RemoteServerURL))
                {
                    return;
                }

                User user = _userManager.GetUserById(e.UserId);
                if (user == null)
                {
                    _logger.LogWarning("UserDataSaved event triggered with unknown user");
                    return;
                }

                UserItemDataDto dto = _userDataRepository.GetUserDataDto(e.Item, user);
                // Not set automatically
                dto.ItemId = e.Item.Id.ToString();

                Uri baseUri = new Uri(Plugin.Instance.Configuration.RemoteServerURL);
                Uri uri = new Uri(baseUri, "/internal/sync/update?userId=" + HttpUtility.UrlEncode(e.UserId.ToString()));
                HttpRequestMessage request = new HttpRequestMessage();
                request.Method = HttpMethod.Post;
                request.RequestUri = uri;
                request.Headers.Add("X-Internal-Sync-Auth", Plugin.Instance.Configuration.AuthToken);
                request.Content = JsonContent.Create(dto);
                client.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error while attempting to sync user data: " + ex.ToString());
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
