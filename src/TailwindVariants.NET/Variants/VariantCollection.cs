using System.Collections;
using System.Linq.Expressions;

namespace TailwindVariants.NET;

/// <summary>
/// Collection of variant definitions keyed by an accessor expression.
/// </summary>
public class VariantCollection<TOwner, TSlots> : IEnumerable<KeyValuePair<Expression<VariantAccessor<TOwner>>, IVariant<TSlots>>>
	where TSlots : ISlots
	where TOwner : ISlottable<TSlots>
{
	private readonly Dictionary<Expression<VariantAccessor<TOwner>>, IVariant<TSlots>> _variants = [];

	/// <summary>
	/// Gets the variant associated with the specified accessor expression.
	/// </summary>
	/// <param name="key">The accessor expression used as the key.</param>
	/// <returns>The variant associated with the specified key.</returns>
	public IVariant<TSlots> this[Expression<VariantAccessor<TOwner>> key]
	{
		get => _variants[key];
		set => _variants[key] = value;
	}

	/// <summary>
	/// Adds a new variant definition to the collection.
	/// </summary>
	/// <param name="key">The accessor expression used as the key.</param>
	/// <param name="value">The variant to associate with the key.</param>
	public void Add(Expression<VariantAccessor<TOwner>> key, IVariant<TSlots> value) => _variants.Add(key, value);

	/// <summary>
	/// Returns an enumerator that iterates through the collection of variant definitions.
	/// </summary>
	/// <returns>An enumerator for the variant collection.</returns>
	public IEnumerator<KeyValuePair<Expression<VariantAccessor<TOwner>>, IVariant<TSlots>>> GetEnumerator() => _variants.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
