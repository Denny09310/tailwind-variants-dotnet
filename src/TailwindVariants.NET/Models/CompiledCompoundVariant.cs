using System.Diagnostics;

using static TailwindVariants.NET.TvHelpers;

namespace TailwindVariants.NET.Models;

/// <summary>
/// Represents a compiled compound variant, which applies when multiple variant
/// conditions are satisfied simultaneously.
/// </summary>
public interface ICompiledCompoundVariant : IApplicableVariant;

internal record struct CompiledCompoundVariant<TOwner, TSlots>(Predicate<TOwner> Predicate, SlotCollection<TSlots> Slots) : ICompiledCompoundVariant
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

			if (!Predicate(owner))
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
