using System.Linq.Expressions;
using System.Reflection;

namespace TailwindVariants.NET.Core;

/// <summary>
/// Compiles fast property accessors for variant resolution.
/// </summary>
internal static class PropertyAccessor
{
	/// <summary>
	/// Compiles a delegate that reads a property value and returns it as a string.
	/// </summary>
	/// <typeparam name="TProps">The props type.</typeparam>
	/// <param name="propertyName">Property name.</param>
	/// <returns>
	/// A delegate returning the property value as string,
	/// or <c>null</c> if the property does not exist.
	/// </returns>
	public static Func<TProps, string?> Compile<TProps>(string propertyName)
	{
		var prop = typeof(TProps).GetProperty(
			propertyName,
			BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

		if (prop is null)
			return _ => null;

		var param = Expression.Parameter(typeof(TProps), "props");
		var access = Expression.Property(param, prop);

		Expression body = prop.PropertyType == typeof(string)
			? access
			: Expression.Call(access, nameof(ToString), Type.EmptyTypes);

		return Expression
			.Lambda<Func<TProps, string?>>(body, param)
			.Compile();
	}
}
