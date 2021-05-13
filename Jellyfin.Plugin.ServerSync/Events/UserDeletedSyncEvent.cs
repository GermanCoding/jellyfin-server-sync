using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Events.Users;
using MediaBrowser.Controller.Events;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ServerSync.Events
{
    class UserDeletedSyncEvent : IEventConsumer<UserDeletedEventArgs>
    {
        private readonly ILogger _logger;
        public UserDeletedSyncEvent(ILogger<UserDeletedSyncEvent> logger)
        {
            _logger = logger;
        }

        public Task OnEvent(UserDeletedEventArgs eventArgs)
        {
            User user = eventArgs.Argument;
            ServerSync.Instance.OnUserDeleted(user.Id);
            return Task.CompletedTask;
        }
    }
}
