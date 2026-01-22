using TailwindVariants.NET.Core;

namespace TailwindVariants.NET.Models;

/// <summary>
/// Represents a compound variant requiring multiple conditions.
/// </summary>
/// <typeparam name="TProps">Variant props type.</typeparam>
/// <remarks>
/// Creates a new <see cref="CompiledCompoundVariant{TProps}"/>.
/// </remarks>
/// <param name="conditions">
/// Variant property/value pairs that must match.
/// </param>
/// <param name="classValue">
/// Class string applied when all conditions are satisfied.
/// </param>
public sealed class CompiledCompoundVariant<TProps>(
	Dictionary<string, string> conditions,
	string classValue) : IApplicableVariant<TProps>
{
	private readonly IReadOnlyList<(Func<TProps, string?> Getter, string Expected)> _conditions =
		conditions
			.Select(c => (PropertyAccessor.Compile<TProps>(c.Key), c.Value))
			.ToList();

	/// <inheritdoc />
	public void Apply(TProps props, List<string> classes)
	{
		foreach (var (getter, expected) in _conditions)
		{
			if (getter(props) != expected)
				return;
		}

		classes.Add(classValue);
	}
}
