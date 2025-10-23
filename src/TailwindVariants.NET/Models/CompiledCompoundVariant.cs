using Microsoft.Extensions.Logging;

using static TailwindVariants.NET.TvHelpers;

namespace TailwindVariants.NET.Models;

/// <summary>
/// Represents a compiled compound variant, which applies when multiple variant
/// conditions are satisfied simultaneously.
/// </summary>
public interface ICompiledCompoundVariant : IApplicableVariant;

internal record struct CompiledCompoundVariant<TOwner, TSlots>(Predicate<TOwner> Predicate, SlotCollection<TSlots> Slots) : ICompiledCompoundVariant
	where TSlots : ISlots
	where TOwner : ISlottable<TSlots>
{
	public readonly void Apply(object obj, Action<string, string> aggregator, ILoggerFactory factory)
    {
        var logger = factory.CreateLogger<CompiledCompoundVariant<TOwner, TSlots>>();

        try
        {
            if (obj is not TOwner owner)
            {
				logger.LogWarning("Variant evaluation skipped due to owner type mismatch. Expected {ExpectedOwner}, got {ActualOwner}.", typeof(TOwner), obj?.GetType());
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
