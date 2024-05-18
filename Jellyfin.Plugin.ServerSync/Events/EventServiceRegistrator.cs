using Jellyfin.Data.Events.Users;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ServerSync.Events
{
    public class EventServiceRegistrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddScoped<IEventConsumer<UserCreatedEventArgs>, UserCreatedSyncEvent>();
            serviceCollection.AddScoped<IEventConsumer<UserDeletedEventArgs>, UserDeletedSyncEvent>();
        }
    }
}
