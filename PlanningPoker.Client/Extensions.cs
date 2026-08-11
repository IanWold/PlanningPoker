using Microsoft.Extensions.DependencyInjection;

namespace PlanningPoker.Client;

public static class Extensions {
    extension(IServiceCollection services) {
        public void AddClient<TEncryption, TTransport>()
            where TEncryption : class, IEncryptionService
            where TTransport : class, ISessionTransport {
            services.AddSingleton<IEncryptionService, TEncryption>();
            services.AddSingleton<ISessionTransport, TTransport>();
            services.AddSingleton<SessionStore>();
            services.AddSingleton<ToastStore>();
            services.AddSingleton<Client>();
        }
    }
}