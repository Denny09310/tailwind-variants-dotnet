using System.Linq.Expressions;

using Microsoft.Extensions.Logging;

using static TailwindVariants.NET.TvHelpers;

namespace TailwindVariants.NET;

/// <summary>
/// Represents a compiled single variant, which applies based on a single variant condition.
/// </summary>
public interface ICompiledVariant : IApplicableVariant;

internal record struct CompiledVariant<TOwner, TSlots>(Expression<VariantAccessor<TOwner>> Expr, IVariant<TSlots> Entry, VariantAccessor<TOwner> Accessor) : ICompiledVariant
	where TSlots : ISlots
	where TOwner : ISlottable<TSlots>
{
	public readonly void Apply(object obj, Action<string, string> aggregator, ILoggerFactory factory)
	{
		var logger = factory.CreateLogger<CompiledVariant<TOwner, TSlots>>();

        try
        {
            if (obj is not TOwner owner)
            {
                var expected = typeof(TOwner).FullName ?? typeof(TOwner).Name;
                var actual = obj?.GetType().FullName ?? "<null>";
                logger.LogWarning(
                    "Variant evaluation skipped due to owner type mismatch. Expected {ExpectedOwner}, got {ActualOwner}.",
                    expected,
                    actual);
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
            logger.LogError(ex, "Variant evaluation failed for '{Expr}'", Expr);
        }
	}
}
