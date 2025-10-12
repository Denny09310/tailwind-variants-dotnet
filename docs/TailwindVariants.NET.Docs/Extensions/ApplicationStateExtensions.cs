using TailwindVariants.NET.Docs.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class ApplicationStateExtensions
{
    public static IServiceCollection AddApplicationState(this IServiceCollection services)
    {
        services.AddSingleton<SidebarState>();
        return services;
    }
}