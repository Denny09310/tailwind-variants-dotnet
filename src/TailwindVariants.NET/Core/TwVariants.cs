using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;

using static TailwindVariants.NET.TvHelpers;

using Tw = TailwindMerge.TwMerge;

namespace TailwindVariants.NET;

/// <summary>
/// Core function factory that builds a Tailwind-variants-like function.
/// </summary>
public class TwVariants(Tw merge)
{
	/// <summary>
	/// Creates a slot map containing the final computed CSS class strings for each slot, based on the provided owner and options.
	/// </summary>
	/// <typeparam name="TOwner">The type that owns the slots and variants.</typeparam>
	/// <typeparam name="TSlots">The type representing the slots, which must implement <see cref="ISlots"/>.</typeparam>
	/// <param name="owner">The instance providing slot and variant values.</param>
	/// <param name="definition">The configuration options for base slots, variants, and compound variants.</param>
	/// <returns>
	/// A <see cref="SlotsMap{TSlots}"/> mapping slot names to their final computed CSS class strings.
	/// </returns>
	/// <remarks>
	/// The returned function is safe to call multiple times; per-call overrides do not mutate precomputed definitions.
	/// </remarks>
	public SlotsMap<TSlots> Invoke<TOwner, TSlots>(TOwner owner, TvDescriptor<TOwner, TSlots> definition)
		where TSlots : ISlots, new()
		where TOwner : ISlotted<TSlots>
	{
		// 1. Start with base slots
		var builders = definition.ComputedSlots.ToDictionary(
			kv => kv.Key,
			kv => new StringBuilder(kv.Value));

		// 3. Apply variants
		builders = ApplyVariants(owner, builders, definition.ComputedVariants);

		// 4. Apply compound variants
		builders = ApplyCompoundVariants(owner, builders, definition.ComputedCompoundVariants);

		// 5. Apply per-instance slot overrides (Classes property)
		ApplySlotsOverrides<TOwner, TSlots>(owner, builders);

		// 6. Build final map
		return builders.ToDictionary(
			kv => kv.Key,
			kv => merge.Merge(kv.Value.ToString()));
	}

	#region Helpers

	private static void AddClass<TSlots>(
		Dictionary<string, StringBuilder> builders,
		Expression<SlotAccessor<TSlots>> accessor,
		string classes) where TSlots : ISlots, new()
	{
		var name = GetSlot(accessor);
		if (!builders.TryGetValue(name, out var builder))
		{
			builder = new StringBuilder();
			builders[name] = builder;
		}
		builder.Append(' ');
		builder.Append(classes);
	}

	private static Dictionary<string, StringBuilder> ApplyCompoundVariants<TOwner, TSlots>(
		TOwner owner,
		Dictionary<string, StringBuilder> builders,
		IReadOnlyCollection<CompiledCompoundVariant<TOwner, TSlots>>? compoundVariants)
		where TSlots : ISlots, new()
		where TOwner : ISlotted<TSlots>
	{
		if (compoundVariants is null)
		{
			return builders;
		}

		foreach (var cv in compoundVariants)
		{
			try
			{
				if (!cv.Predicate(owner))
				{
					continue;
				}

				foreach (var (slot, value) in cv.Slots)
				{
					if (slot is null)
					{
						continue;
					}

					AddClass(builders, slot, value.ToString());
				}
			}
			catch (Exception ex)
			{
				// keep robust but log for debugging
				Debug.WriteLine($"Compound variant predicate or processing failed: {ex.Message}");
			}
		}

		return builders;
	}

	private static void ApplySlotsOverrides<TOwner, TSlots>(TOwner owner, Dictionary<string, StringBuilder> builders)
				where TOwner : ISlotted<TSlots>
		where TSlots : ISlots, new()
	{
		if (owner.Classes is not null)
		{
			foreach (var (slot, value) in owner.Classes.EnumerateOverrides())
			{
				if (!builders.TryGetValue(slot, out var builder))
				{
					builder = new StringBuilder();
					builders[slot] = builder;
				}
				builder.Append(' ');
				builder.Append(value);
			}
		}
	}

	private static Dictionary<string, StringBuilder> ApplyVariants<TOwner, TSlots>(
		TOwner owner,
		Dictionary<string, StringBuilder> builders,
		IReadOnlyCollection<CompiledVariant<TOwner, TSlots>> variants)
		where TSlots : ISlots, new()
		where TOwner : ISlotted<TSlots>
	{
		foreach (var compiled in variants)
		{
			try
			{
				var selected = compiled.Accessor(owner);
				if (selected is null) continue;

				if (compiled.Entry.TryGetSlots(selected, out var slots) && slots is not null)
				{
					foreach (var (slot, value) in slots)
					{
						AddClass(builders, slot, value.ToString());
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Variant evaluation failed for '{compiled.Expr}': {ex.Message}");
			}
		}

		if (!string.IsNullOrEmpty(owner.Class))
		{
			AddClass<TSlots>(builders, s => s.Base, owner.Class);
		}

		return builders;
	}

	#endregion Helpers
}
