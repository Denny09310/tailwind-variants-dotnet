using System.Diagnostics;
using System.Linq.Expressions;

using static TailwindVariants.NET.TvHelpers;

namespace TailwindVariants.NET;

/// <summary>
/// Represents a compiled single variant, which applies based on a single variant condition.
/// </summary>
public interface ICompiledVariant : IApplicableVariant;

internal record struct CompiledVariant<TOwner, TSlots>(Expression<VariantAccessor<TOwner>> Expr, IVariant<TSlots> Entry, VariantAccessor<TOwner> Accessor) : ICompiledVariant
	where TSlots : ISlots, new()
	where TOwner : ISlotted<TSlots>
{
	public readonly void Apply(object obj, Action<string, string> aggregator)
	{
		try
		{
			// Should I throw error for mismatching component?
			if (obj is not TOwner owner)
			{
				return;
			}

			var selected = Accessor(owner);
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
