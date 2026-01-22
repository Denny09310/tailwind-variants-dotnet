using TailwindVariants.NET.Core;

namespace TailwindVariants.NET.Models;

/// <summary>
/// Represents a single compiled variant.
/// </summary>
/// <typeparam name="TProps">Variant props type.</typeparam>
/// <remarks>
/// Creates a new <see cref="CompiledVariant{TProps}"/>.
/// </remarks>
/// <param name="name">Variant property name.</param>
/// <param name="values">Variant value to class mapping.</param>
public sealed class CompiledVariant<TProps>(
	string name,
	Dictionary<string, ClassValue> values) : IApplicableVariant<TProps>
{
	private readonly Func<TProps, string?> _getter = PropertyAccessor.Compile<TProps>(name);

	/// <inheritdoc />
	public void Apply(TProps props, List<string> classes)
	{
		var value = _getter(props);
		if (value is null)
			return;

		if (values.TryGetValue(value, out var classValue))
			classes.Add(classValue.Value);
	}
}
