using Microsoft.Extensions.Logging;

using static TailwindVariants.NET.TvHelpers;

namespace TailwindVariants.NET.Models;

/// <summary>
/// Represents a compiled compound variant, which applies when multiple variant
/// conditions are satisfied simultaneously.
/// </summary>
public interface ICompiledCompoundVariant : IApplicableVariant;

internal record struct CompiledCompoundVariant<TOwner, TSlots>(Predicate<TOwner> Predicate, SlotCollection<TSlots> Slots) : ICompiledCompoundVariant
	where TSlots : ISlots, new()
	where TOwner : ISlottable<TSlots>
{
	public readonly void Apply(object obj, Action<string, string> aggregator, ILoggerFactory factory)
	{
		var logger = factory.CreateLogger<CompiledVariant<TOwner, TSlots>>();

		try
		{
			// Should I throw error for mismatching component?
			if (obj is not TOwner owner)
			{
				logger.LogWarning("");
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
			logger.LogError(ex, "Compound variant predicate or processing failed");
		}
	}
}
