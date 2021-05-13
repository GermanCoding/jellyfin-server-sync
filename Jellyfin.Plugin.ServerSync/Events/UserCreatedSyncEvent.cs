using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Events.Users;
using MediaBrowser.Controller.Events;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ServerSync
{
    public class UserCreatedSyncEvent : IEventConsumer<UserCreatedEventArgs>
    {
        private readonly ILogger _logger;

        public UserCreatedSyncEvent(ILogger<UserCreatedSyncEvent> logger)
        {
            _logger = logger;
        }

        public Task OnEvent(UserCreatedEventArgs eventArgs)
        {
            User user = eventArgs.Argument;
            ServerSync.Instance.OnUserCreated(user);
            return Task.CompletedTask;
        }
    }
}
