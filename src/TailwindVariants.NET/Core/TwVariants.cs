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
    /// Create a Tv function using the provided options. The returned function is safe to call multiple times;
    /// per-call overrides do not mutate precomputed definitions.
    /// </summary>
    public SlotsMap<TSlots> Invoke<TOwner, TSlots>(TOwner owner, TvOptions<TOwner, TSlots> definition)
        where TSlots : ISlots, new()
        where TOwner : ISlotted<TSlots>
    {
        // 1. Start with base slots
        var builders = definition.BaseSlots.ToDictionary(
            kv => kv.Key,
            kv => new StringBuilder(kv.Value));

        // 2. Apply variants
        builders = ApplyVariants(owner, builders, definition.BaseVariants);

        // 3. Apply compound variants
        builders = ApplyCompoundVariants(definition, owner, builders);

        // 4. Apply per-instance slot overrides (Classes property)
        if (owner.Classes is not null)
        {
            foreach (var (slot, value) in EnumerateClassesOverrides(owner.Classes))
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

        // 5. Build final map
        return builders.ToDictionary(
            kv => kv.Key,
            kv => merge.Merge(kv.Value.ToString()));
    }

    #region Helpers

    private static void AddSlotClass<TSlots>(
        Dictionary<string, StringBuilder> builders,
        Expression<SlotAccessor<TSlots>> accessor,
        string classes) where TSlots : ISlots
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
        TvOptions<TOwner, TSlots>? options,
        TOwner owner,
        Dictionary<string, StringBuilder> builders)
        where TSlots : ISlots, new()
        where TOwner : ISlotted<TSlots>
    {
        if (options?.CompoundVariants is null)
        {
            return builders;
        }

        foreach (var cv in options.CompoundVariants)
        {
            try
            {
                if (!cv.Predicate(owner))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(cv.Class))
                {
                    AddSlotClass<TSlots>(builders, s => s.Base, cv.Class);
                }

                foreach (var pairs in cv)
                {
                    var slots = pairs.Value;

                    if (slots is null)
                    {
                        continue;
                    }

                    foreach (var slotKv in slots)
                    {
                        AddSlotClass(builders, slotKv.Key, (string)slotKv.Value);
                    }
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

    private static Dictionary<string, StringBuilder> ApplyVariants<TOwner, TSlots>(
        TOwner owner,
        Dictionary<string, StringBuilder> builders,
        IReadOnlyDictionary<string, CompiledVariant<TOwner, TSlots>> baseVariants)
        where TSlots : ISlots, new()
        where TOwner : ISlotted<TSlots>
    {
        if (!string.IsNullOrEmpty(owner.Class))
        {
            AddSlotClass<TSlots>(builders, s => s.Base, owner.Class);
        }

        foreach (var compiled in baseVariants.Values)
        {
            try
            {
                var selected = compiled.Accessor(owner);
                if (selected is null) continue;

                if (compiled.Entry.TryGetSlots(selected, out var slots) && slots is not null)
                {
                    foreach (var kv in slots)
                    {
                        AddSlotClass(builders, kv.Key, (string)kv.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Variant evaluation failed for '{compiled.Expr}': {ex.Message}");
            }
        }

        return builders;
    }

    #endregion Helpers
}