using System.Linq.Expressions;
using System.Text;

using Microsoft.Extensions.Logging;

using TailwindVariants.NET.Models;

using static TailwindVariants.NET.TvHelpers;

using Tw = TailwindMerge.TwMerge;

namespace TailwindVariants.NET;

/// <summary>
/// Core function factory that builds a Tailwind-variants-like function.
/// </summary>
public class TwVariants(ILoggerFactory factory, Tw merge)
{
	/// <summary>
	/// Creates a slot map containing the final computed CSS class strings for each slot, based on the provided owner and options.
	/// </summary>
	/// <typeparam name="TOwner">The type that owns the slots and variants.</typeparam>
	/// <typeparam name="TSlots">The type representing the slots, which must implement <see cref="ISlots"/>.</typeparam>
	/// <param name="owner">The instance providing slot and variant values.</param>
	/// <param name="descriptor">The configuration options for base slots, variants, and compound variants.</param>
	/// <returns>
	/// A <see cref="SlotsMap{TSlots}"/> mapping slot names to their final computed CSS class strings.
	/// </returns>
	/// <remarks>
	/// The returned function is safe to call multiple times; per-call overrides do not mutate precomputed definitions.
	/// </remarks>
	public SlotsMap<TSlots> Invoke<TOwner, TSlots>(TOwner owner, TvDescriptor<TOwner, TSlots> descriptor)
		where TSlots : ISlots, new()
		where TOwner : ISlotted<TSlots>
	{
		var generic = (ITvDescriptor)descriptor;

		// 1. Start with base slots
		var builders = generic.Slots.ToDictionary(
			kv => kv.Key,
			kv => new StringBuilder(kv.Value));

		// 3. Apply variants
		ApplyVariants<TOwner, TSlots>(owner, builders, generic.Variants, factory);

		// 4. Apply compound variants
		ApplyCompoundVariants<TOwner, TSlots>(owner, builders, generic.CompoundVariants, factory);

		// 5. Apply per-instance slot overrides (Classes property)
		ApplySlotsOverrides<TOwner, TSlots>(owner, builders);

		// 6. Build final map
		return builders.ToDictionary(
			kv => kv.Key,
			kv => merge.Merge(kv.Value.ToString()));
	}

	#region Helpers

	private static void AddClass<TSlots>(
		Dictionary<string, StringBuilder> builders, Expression<SlotAccessor<TSlots>> accessor, string classes)
		where TSlots : ISlots, new()
	{
		var name = GetSlot(accessor);
		AddClass<TSlots>(builders, name, classes);
	}

	private static void AddClass<TSlots>(
		Dictionary<string, StringBuilder> builders, string slot, string classes)
		where TSlots : ISlots, new()
	{
		if (!builders.TryGetValue(slot, out var builder))
		{
			builder = new StringBuilder();
			builders[slot] = builder;
		}
		builder.Append(' ');
		builder.Append(classes);
	}

	private static void ApplyCompoundVariants<TOwner, TSlots>(
		TOwner owner, Dictionary<string, StringBuilder> builders, IReadOnlyCollection<ICompiledCompoundVariant>? compounds, ILoggerFactory factory)
		where TSlots : ISlots, new()
		where TOwner : ISlotted<TSlots>
	{
		if (compounds is null)
		{
			return;
		}

		foreach (var cv in compounds)
		{
			cv.Apply(owner, (slot, value) => AddClass<TSlots>(builders, slot, value), factory);
		}
	}

	private static void ApplySlotsOverrides<TOwner, TSlots>(
			TOwner owner, Dictionary<string, StringBuilder> builders)
		where TOwner : ISlotted<TSlots>
		where TSlots : ISlots, new()
	{
		if (owner.Classes is null)
		{
			return;
		}

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

	private static void ApplyVariants<TOwner, TSlots>(
		TOwner owner, Dictionary<string, StringBuilder> builders, IReadOnlyCollection<ICompiledVariant>? variants, ILoggerFactory factory)
		where TSlots : ISlots, new()
		where TOwner : ISlotted<TSlots>
	{
		if (variants is not null)
		{
			foreach (var variant in variants)
			{
				variant.Apply(owner, (slot, value) => AddClass<TSlots>(builders, slot, value), factory);
			}
		}

		if (!string.IsNullOrEmpty(owner.Class))
		{
			AddClass<TSlots>(builders, s => s.Base, owner.Class);
		}
	}

	#endregion Helpers
}
