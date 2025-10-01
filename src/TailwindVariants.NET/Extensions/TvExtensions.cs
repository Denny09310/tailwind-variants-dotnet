using TailwindMerge.Extensions;
using TailwindVariants.NET;

namespace Microsoft.Extensions.DependencyInjection;

public static class TvExtensions
{
    public static IServiceCollection AddTailwindVariants(this IServiceCollection services)
    {
        services.AddTailwindMerge();
        services.AddSingleton<TwVariants>();
        return services;
    }
}