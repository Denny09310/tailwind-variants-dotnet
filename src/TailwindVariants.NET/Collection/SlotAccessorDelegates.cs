namespace TailwindVariants.NET;

/// <summary>
/// Represents a delegate that retrieves the CSS classes for a specific slot from a typed slots object.
/// </summary>
/// <typeparam name="TSlots">
/// The type that defines the available slots, implementing <see cref="ISlots"/>.
/// </typeparam>
/// <param name="slots">
/// The slots instance from which to access the slot’s CSS classes.
/// </param>
/// <returns>
/// The CSS class string associated with the slot, or <c>null</c> if no classes are defined.
/// </returns>
public delegate string? SlotAccessor<TSlots>(TSlots slots)
	where TSlots : ISlots, new();

/// <summary>
/// Represents a delegate that retrieves a variant value from a given owner instance.
/// </summary>
/// <typeparam name="TOwner">
/// The type that owns or defines the variant.
/// </typeparam>
/// <param name="owner">
/// The instance from which to extract the variant value.
/// </param>
/// <returns>
/// The selected variant object, or <c>null</c> if no variant is defined.
/// </returns>
public delegate object? VariantAccessor<TOwner>(TOwner owner);

/// <summary>
/// Represents a delegate that transforms or combines slot CSS class strings.
/// </summary>
/// <param name="classes">
/// The input CSS class string to be transformed or combined.
/// </param>
/// <returns>
/// The resulting CSS class string after transformation, or <c>null</c> if no result is produced.
/// </returns>
public delegate string? ClassesAggregator(string? classes);
