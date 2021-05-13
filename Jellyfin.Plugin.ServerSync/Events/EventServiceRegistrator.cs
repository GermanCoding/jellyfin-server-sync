using Jellyfin.Data.Events.Users;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ServerSync.Events
{
    public class EventServiceRegistrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IEventConsumer<UserCreatedEventArgs>, UserCreatedSyncEvent>();
            serviceCollection.AddScoped<IEventConsumer<UserDeletedEventArgs>, UserDeletedSyncEvent>();
        }
    }
}
