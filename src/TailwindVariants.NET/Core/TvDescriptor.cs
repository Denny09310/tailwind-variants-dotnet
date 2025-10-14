using TailwindVariants.NET.Models;

using static TailwindVariants.NET.TvHelpers;

namespace TailwindVariants.NET;

/// <summary>
/// Non-generic interface for TailwindVariants descriptors, enabling polymorphic access to configuration.
/// </summary>
public interface ITvDescriptor
{
	/// <summary>
	/// Gets the collection of compiled compound variants that define
	/// conditional style combinations based on multiple variant values.
	/// </summary>
	IReadOnlyCollection<ICompiledCompoundVariant>? CompoundVariants { get; }

	/// <summary>
	/// Gets the parent descriptor from which this descriptor inherits configuration.
	/// </summary>
	ITvDescriptor? Extends { get; }

	/// <summary>
	/// Gets the computed slot-to-CSS class mappings, including inherited slots.
	/// </summary>
	IReadOnlyDictionary<string, string> Slots { get; }

	/// <summary>
	/// Gets the collection of compiled variants that define individual style variations.
	/// </summary>
	IReadOnlyCollection<ICompiledVariant>? Variants { get; }
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
	private IReadOnlyCollection<ICompiledCompoundVariant>? _compounds;
	private IReadOnlyDictionary<string, string> _slots = default!;
	private IReadOnlyCollection<ICompiledVariant>? _variants;

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
	/// Gets the precomputed compound variants definitions.
	/// This collection is populated during initialization by compiling compound variant expressions and merging with extended variants.
	/// </summary>

	IReadOnlyCollection<ICompiledCompoundVariant>? ITvDescriptor.CompoundVariants => _compounds;

	/// <summary>
	/// Gets the precomputed slot-to-CSS class mappings, including inherited slots from parent descriptors.
	/// This dictionary is populated during initialization by merging base classes, top-level slots, and extended slots.
	/// </summary>
	IReadOnlyDictionary<string, string> ITvDescriptor.Slots => _slots;

	/// <summary>
	/// Gets the precomputed variant definitions with compiled accessors for runtime evaluation.
	/// This collection is populated during initialization by compiling variant expressions and merging with extended variants.
	/// </summary>
	IReadOnlyCollection<ICompiledVariant>? ITvDescriptor.Variants => _variants;

	#endregion Explicit Implementations

	#region Precompute helpers

	/// <summary>
	/// Generic helper that walks the extends chain (immediate -> ancestor -> ancestor...) and
	/// collects per-descriptor collections in the *same append order* as the original implementation.
	/// </summary>
	private static List<T>? InheritFromAncestors<T>(ITvDescriptor? extends, Func<ITvDescriptor, IReadOnlyCollection<T>?> selector)
	{
		if (extends is null) return null;

		List<T>? result = null;
		var current = extends;

		// The original recursive implementation added immediate extends first,
		// then their extends, etc. Appending in this loop reproduces that exact order.
		while (current != null)
		{
			var items = selector(current);
			if (items != null)
			{
				result ??= [];
				foreach (var it in items) result.Add(it);
			}
			current = current.Extends;
		}

		return result;
	}

	private void Precompute()
	{
		_slots = PrecomputeBaseAndTopLevelSlots();
		_variants = PrecomputeVariantDefinitions();
		_compounds = PrecomputeCompoundVariantsDefinitions();
	}

	/// <summary>
	/// Recreates the original "precompute base and top-level slots" semantics:
	/// - current descriptor's Base and Slots are present (as before)
	/// - then each extends' Slots are inserted so that farthest ancestor ends up first in the
	///   final ordering (exactly the same ordering as original recursion that used Insert(0)).
	/// </summary>
	private Dictionary<string, string> PrecomputeBaseAndTopLevelSlots()
	{
		// same as original map type
		var map = new Dictionary<string, ClassValue>(StringComparer.Ordinal);

		// Add current descriptor's Slots and Base (same as original: current values are present)
		if (Slots is not null)
		{
			foreach (var (key, value) in Slots)
			{
				map[GetSlot(key)] = value;
			}
		}

		if (Base is not null)
		{
			map[GetSlot<TSlots>(s => s.Base)] = Base;
		}

		// Merge extends chain: for each ancestor (immediate -> parent -> ...),
		// insert ancestor values at the front of existing ClassValue (Insert(0, ...))
		// to preserve the original ordering (farthest ancestor comes first).
		var cur = Extends;
		while (cur != null)
		{
			if (cur.Slots is not null)
			{
				foreach (var (key, value) in cur.Slots)
				{
					var name = key;
					if (!map.TryGetValue(name, out var classValue))
					{
						classValue = [];
						map[name] = classValue;
					}
					// Insert at 0 exactly as the original recursion did
					classValue.Insert(0, value);
				}
			}
			cur = cur.Extends;
		}

		// Convert to the same final dictionary shape (string values are ToString() of ClassValue)
		return map.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
	}

	private List<ICompiledCompoundVariant>? PrecomputeCompoundVariantsDefinitions()
	{
		List<ICompiledCompoundVariant>? compounds = null;

		if (CompoundVariants is not null)
		{
			compounds = [];
			foreach (var cv in CompoundVariants)
			{
				if (!string.IsNullOrEmpty(cv.Class))
				{
					cv.Slots.Add(cv.Class);
				}
				compounds.Add(new CompiledCompoundVariant<TOwner, TSlots>(cv.Predicate, cv.Slots));
			}
		}

		// Append inherited compiled compound variants in the same order as original
		var inherited = InheritFromAncestors(Extends, d => d.CompoundVariants);
		if (inherited != null)
		{
			compounds ??= [];
			compounds.AddRange(inherited);
		}

		return compounds;
	}

	private List<ICompiledVariant>? PrecomputeVariantDefinitions()
	{
		List<ICompiledVariant>? variants = null;

		if (Variants is not null)
		{
			variants = [];
			foreach (var (key, value) in Variants)
			{
				var accessor = key.Compile();
				variants.Add(new CompiledVariant<TOwner, TSlots>(key, value, accessor));
			}
		}

		// Append inherited compiled variants in the same order as original
		var inherited = InheritFromAncestors(Extends, d => d.Variants);
		if (inherited != null)
		{
			variants ??= [];
			variants.AddRange(inherited);
		}

		return variants;
	}

	#endregion Precompute helpers
}
