using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Events;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ServerSync
{
    public class ServerSync : IServerEntryPoint
    {
        public static ServerSync Instance { get; set; }

        private static readonly HttpClient _client = new HttpClient();

        private readonly IUserManager _userManager;
        private readonly ILogger _logger;
        private readonly IUserDataManager _userDataRepository;
        private readonly IServerApplicationHost _appHost;
        private readonly ISessionManager _sessionManager;
        private readonly Uri _baseUri;
        private readonly List<string> _createdUsersByName;
        private readonly List<Guid> _deletedUsers;
        private ReflectionHelper ReflectionHelper { get; set; }

        public ServerSync(IUserManager userManager, ILogger<ServerSync> logger, IUserDataManager userDataRepository, IServerApplicationHost appHost, ISessionManager sessionManager)
        {
            Instance = this;
            _userManager = userManager;
            _logger = logger;
            _userDataRepository = userDataRepository;
            _appHost = appHost;
            _sessionManager = sessionManager;
            ReflectionHelper = new ReflectionHelper();
            _baseUri = new Uri(Plugin.Instance.Configuration.RemoteServerURL);
            _deletedUsers = new List<Guid>();
            _createdUsersByName = new List<string>();
        }

        public Task RunAsync()
        {
            _userManager.OnUserUpdated += UserConfigUdated;
            _userDataRepository.UserDataSaved += UserDataSaved;
            _client.DefaultRequestHeaders.Add("User-Agent", "Jellyfin ServerSync");
            return Task.CompletedTask;
        }

        public void OnUserCreated(User user)
        {
            if (_createdUsersByName.Contains(user.Username))
            {
                _createdUsersByName.Remove(user.Username);
                return;
            }

            // Let's wait some time, as the user might get edited after creation
            // Do this on a different thread, because we want to be certain that execution continues
            new Thread(() =>
            {
                // Ugly hack
                Thread.Sleep(5000);
                UserDto dto = _userManager.GetUserDto(user);
                Uri uri = new Uri(_baseUri, "/internal/sync/createUser");
                SendRequest(uri, JsonContent.Create(dto));
            }).Start();
        }

        public void OnUserDeleted(Guid userId)
        {
            if (_deletedUsers.Contains(userId))
            {
                _deletedUsers.Remove(userId);
                return;
            }

            Uri uri = new Uri(_baseUri, "/internal/sync/deleteUser");
            SendRequest(uri, JsonContent.Create(userId));
        }

        // Currently only called when user renamed
        private void UserConfigUdated(object sender, GenericEventArgs<User> e)
        {
            _logger.LogInformation("UserConfigUpdated() !!!");
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
                Uri uri = new Uri(_baseUri, "/internal/sync/update?userId=" + HttpUtility.UrlEncode(e.UserId.ToString()));
                SendRequest(uri, JsonContent.Create(dto));
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error while attempting to sync user data: " + ex.ToString());
            }
        }

        public void SyncUserDeletion(Guid userId)
        {
            _deletedUsers.Add(userId);
            // Do the same as what UserController does on deletion
            _sessionManager.RevokeUserTokens(userId, null);
            _userManager.DeleteUserAsync(userId);
        }

        public async void SyncUserCreation(UserDto data)
        {
            _createdUsersByName.Add(data.Name);
            // This requires a custom build of Jellyfin that has this method
            User newUser = await _userManager.CreateUserAsync(data.Name, data.Id).ConfigureAwait(true);
            UpdateUserConfiguration(newUser, data);
        }

        public async void UpdateUserConfiguration(User newUser, UserDto data)
        {
            await _userManager.UpdateConfigurationAsync(newUser.Id, data.Configuration).ConfigureAwait(false);
            await _userManager.UpdatePolicyAsync(newUser.Id, data.Policy).ConfigureAwait(false);
        }

        private static void SendRequest(Uri finalUri, HttpContent content)
        {
            HttpRequestMessage request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = finalUri;
            request.Headers.Add("X-Internal-Sync-Auth", Plugin.Instance.Configuration.AuthToken);
            request.Content = content;
            _client.SendAsync(request).ConfigureAwait(false);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
