using System.Diagnostics;
using System.Linq.Expressions;

using static TailwindVariants.NET.TvHelpers;

namespace TailwindVariants.NET;

public interface IApplicableVariant
{
	void Apply(object owner, Action<string, string> aggregator);
}

public interface ICompiledCompoundVariant : IApplicableVariant;

public interface ICompiledVariant : IApplicableVariant;

internal record struct CompiledVariant<TOwner, TSlots>(Expression<VariantAccessor<TOwner>> Expr, IVariant<TSlots> Entry, VariantAccessor<TOwner> Accessor) : ICompiledVariant
	where TSlots : ISlots, new()
	where TOwner : ISlotted<TSlots>
{
	public readonly void Apply(object owner, Action<string, string> aggregator)
	{
		try
		{
			var selected = Accessor((TOwner)owner);
			if (selected is null) return;

			if (Entry.TryGetSlots(selected, out var slots) && slots is not null)
			{
				foreach (var (slot, value) in slots)
				{
					aggregator(GetSlot(slot), value.ToString());
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Variant evaluation failed for '{Expr}': {ex.Message}");
		}
	}
}

internal record struct CompiledCompoundVariant<TOwner, TSlots>(Predicate<TOwner> Predicate, SlotCollection<TSlots> Slots) : ICompiledCompoundVariant
	where TSlots : ISlots, new()
	where TOwner : ISlotted<TSlots>
{
	public readonly void Apply(object owner, Action<string, string> aggregator)
	{
		try
		{
			if (!Predicate((TOwner)owner))
			{
				return;
			}

			foreach (var (slot, value) in Slots)
			{
				if (slot is null)
				{
					continue;
				}

				aggregator(GetSlot(slot), value.ToString());
			}
		}
		catch (Exception ex)
		{
			// keep robust but log for debugging
			Debug.WriteLine($"Compound variant predicate or processing failed: {ex.Message}");
		}
	}
}
