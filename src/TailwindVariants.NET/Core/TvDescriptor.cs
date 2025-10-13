using static TailwindVariants.NET.TvHelpers;

namespace TailwindVariants.NET;

/// <summary>
/// Non-generic interface for TailwindVariants descriptors, enabling polymorphic access to configuration.
/// </summary>
public interface ITvDescriptor
{
	/// <summary>
	/// Gets the parent descriptor from which this descriptor inherits configuration.
	/// </summary>
	ITvDescriptor? Extends { get; }

	/// <summary>
	/// Gets the computed slot-to-CSS class mappings, including inherited slots.
	/// </summary>
	IReadOnlyDictionary<string, string> Slots { get; }
}

/// <summary>
/// Represents the configuration options for TailwindVariants, including base classes, slots, variants, and compound variants.
/// </summary>
/// <typeparam name="TOwner">The type that owns the slots and variants.</typeparam>
/// <typeparam name="TSlots">The type representing the slots, which must implement <see cref="ISlots"/>.</typeparam>
public sealed class TvDescriptor<TOwner, TSlots> : ITvDescriptor
	where TSlots : ISlots, new()
	where TOwner : ISlotted<TSlots>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TvDescriptor{TOwner, TSlots}"/> class.
	/// </summary>
	/// <param name="extends">The parent descriptor to inherit configuration from.</param>
	/// <param name="base">The base CSS classes to apply to the base slot.</param>
	/// <param name="slots">A collection mapping slot accessors to their corresponding CSS class values.</param>
	/// <param name="variants">A collection of variant definitions, each keyed by an accessor expression.</param>
	/// <param name="compoundVariants">A collection of compound variants, which apply additional classes based on specific predicates.</param>
	public TvDescriptor(
		ITvDescriptor? extends = null,
		ClassValue? @base = null,
		SlotCollection<TSlots>? slots = null,
		VariantCollection<TOwner, TSlots>? variants = null,
		CompoundVariantCollection<TOwner, TSlots>? compoundVariants = null)
	{
		Extends = extends;
		Base = @base;
		Slots = slots;
		Variants = variants;
		CompoundVariants = compoundVariants;

		Precompute();
	}

	/// <summary>
	/// The base CSS classes to apply to the base slot.
	/// </summary>
	public ClassValue? Base { get; }

	/// <summary>
	/// A collection of compound variants, which apply additional classes based on specific predicates.
	/// </summary>
	public CompoundVariantCollection<TOwner, TSlots>? CompoundVariants { get; }

	/// <summary>
	/// Gets the parent descriptor from which this descriptor inherits configuration.
	/// </summary>
	public ITvDescriptor? Extends { get; }

	/// <summary>
	/// A collection mapping slot accessors to their corresponding CSS class values.
	/// </summary>
	public SlotCollection<TSlots>? Slots { get; }

	/// <summary>
	/// A collection of variant definitions, each keyed by an accessor expression.
	/// </summary>
	public VariantCollection<TOwner, TSlots>? Variants { get; }

	#region Explicit Implementations

	/// <summary>
	/// Gets the computed slot-to-CSS class mappings, including inherited slots.
	/// </summary>
	IReadOnlyDictionary<string, string> ITvDescriptor.Slots
		=> ComputedSlots;

	#endregion Explicit Implementations

	/// <summary>
	/// Gets the precomputed slot-to-CSS class mappings, including inherited slots from parent descriptors.
	/// This dictionary is populated during initialization by merging base classes, top-level slots, and extended slots.
	/// </summary>
	internal IReadOnlyDictionary<string, string> ComputedSlots { get; private set; } = default!;

	/// <summary>
	/// Gets the precomputed variant definitions with compiled accessors for runtime evaluation.
	/// This dictionary is populated during initialization by compiling variant expressions and merging with extended variants.
	/// </summary>
	internal IReadOnlyDictionary<string, CompiledVariant<TOwner, TSlots>> ComputedVariants { get; private set; } = default!;

	/// <summary>
	/// Recursively merges slot definitions from extended descriptors into the provided map.
	/// Slots from parent descriptors are inserted at the beginning to allow child descriptors to override them.
	/// </summary>
	/// <param name="map">The dictionary to populate with slot-to-CSS class mappings.</param>
	/// <param name="extends">The parent descriptor to inherit slots from.</param>
	private static void PrecomputeExtendsBaseAndTopLevelSlots(Dictionary<string, ClassValue> map, ITvDescriptor? extends = null)
	{
		if (extends is null)
		{
			return;
		}

		if (extends.Slots is not null)
		{
			foreach (var (key, value) in extends.Slots)
			{
				if (!map.TryGetValue(key, out var @class))
				{
					@class = [];
					map[key] = @class;
				}

				@class.Insert(0, value);
			}
		}

		if (extends.Extends is not null)
		{
			PrecomputeExtendsBaseAndTopLevelSlots(map, extends.Extends);
		}
	}

	/// <summary>
	/// Recursively merges variant definitions from extended descriptors into the provided dictionary.
	/// This method is intended to inherit variants from parent descriptors.
	/// </summary>
	/// <param name="variants">The dictionary to populate with variant definitions.</param>
	/// <param name="extends">The parent descriptor to inherit variants from.</param>
	private static void PrecomputeExtendsVariantDefinitions(
		Dictionary<string, CompiledVariant<TOwner, TSlots>> variants,
		ITvDescriptor? extends = null)
	{
		if (extends is null)
		{
			return;
		}

		// TODO: Add variants inheritance

		if (extends.Extends is not null)
		{
			PrecomputeExtendsVariantDefinitions(variants, extends.Extends);
		}
	}

	/// <summary>
	/// Precomputes both slot and variant definitions during descriptor initialization.
	/// This ensures that all inheritance is resolved upfront for optimal runtime performance.
	/// </summary>
	private void Precompute()
	{
		ComputedSlots = PrecomputeBaseAndTopLevelSlots();
		ComputedVariants = PrecomputeVariantDefinitions();
	}

	/// <summary>
	/// Computes the final slot-to-CSS class mappings by merging base classes, slots, and extended slots.
	/// Child descriptor slots take precedence over parent slots when conflicts occur.
	/// </summary>
	/// <returns>A dictionary mapping slot names to their computed CSS class strings.</returns>
	private Dictionary<string, string> PrecomputeBaseAndTopLevelSlots()
	{
		var map = new Dictionary<string, ClassValue>(StringComparer.Ordinal);

		if (Base is not null)
		{
			map[GetSlot<TSlots>(s => s.Base)] = Base.ToString();
		}

		if (Slots is not null)
		{
			foreach (var (key, value) in Slots)
			{
				map[GetSlot(key)] = value.ToString();
			}
		}

		if (Extends is not null)
		{
			PrecomputeExtendsBaseAndTopLevelSlots(map, Extends);
		}

		return map.ToDictionary(
			kvp => kvp.Key,
			kvp => kvp.Value.ToString());
	}

	/// <summary>
	/// Computes the final variant definitions by compiling variant accessors and merging with extended variants.
	/// Each variant is compiled into a delegate for efficient runtime evaluation.
	/// </summary>
	/// <returns>A dictionary mapping variant identifiers to their compiled variant definitions.</returns>
	private Dictionary<string, CompiledVariant<TOwner, TSlots>> PrecomputeVariantDefinitions()
	{
		var variants = new Dictionary<string, CompiledVariant<TOwner, TSlots>>(StringComparer.Ordinal);

		if (Variants is not null)
		{
			foreach (var (key, value) in Variants)
			{
				var id = GetVariant(key);
				var accessor = key.Compile();
				variants[id] = new CompiledVariant<TOwner, TSlots>(key, value, accessor);
			}
		}

		if (Extends is not null)
		{
			PrecomputeExtendsVariantDefinitions(variants, Extends);
		}

		return variants;
	}
}
