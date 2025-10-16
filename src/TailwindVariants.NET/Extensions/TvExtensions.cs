using TailwindMerge;
using TailwindMerge.Extensions;

using TailwindVariants.NET;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for registering TailwindVariants services in the dependency injection container.
/// </summary>
public static class TvExtensions
{
	/// <summary>
	/// Adds TailwindVariants and its dependencies to the specified <see cref="IServiceCollection"/>.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
	/// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
	public static IServiceCollection AddTailwindVariants(this IServiceCollection services)
	{
		services.AddTailwindMerge();
		services.AddSingleton<TwVariants>();
		return services;
	}

	/// <summary>
	/// Adds TailwindVariants and its dependencies to the specified <see cref="IServiceCollection"/>.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
	/// <param name="options">The delegate to configure the <see cref="TwMergeConfig"/> options.</param>
	/// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
	public static IServiceCollection AddTailwindVariants(this IServiceCollection services, Action<TwMergeConfig> options)
	{
		services.AddTailwindMerge(options);
		services.AddSingleton<TwVariants>();
		return services;
	}
}
