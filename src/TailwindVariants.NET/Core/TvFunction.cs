using System.Diagnostics;
using System.Linq.Expressions;
using static TailwindVariants.NET.TvHelpers;

namespace TailwindVariants.NET;

/// <summary>
/// Core function factory that builds a Tailwind-variants-like function.
/// </summary>
public static class TvFunction
{
    /// <summary>
    /// Create a Tv function using the provided options. The returned function is safe to call multiple times;
    /// per-call overrides do not mutate precomputed definitions.
    /// </summary>
    public static TvReturnType<TOwner, TSlots> Tv<TOwner, TSlots>(TvOptions<TOwner, TSlots> options)
        where TSlots : ISlots, new()
        where TOwner : ISlotted<TSlots>
    {
        var baseClasses = PrecomputeBaseAndTopLevelSlots(options);
        var baseVariants = PrecomputeVariantDefinitions(options);

        return (owner, merge) =>
        {
            // 1. Start with base slots
            var builders = baseClasses.ToDictionary(
                kv => kv.Key,
                kv => new CssBuilder(kv.Value));

            // 2. Apply variants
            builders = ApplyVariants(owner, builders, baseVariants);

            // 3. Apply compound variants
            builders = ApplyCompoundVariants(options, owner, builders);

            // 4. Apply per-instance slot overrides (Classes property)
            if (owner.Classes is not null)
            {
                foreach (var (slot, value) in EnumerateClassesOverrides(owner.Classes))
                {
                    if (!builders.TryGetValue(slot, out var builder))
                    {
                        builder = new CssBuilder();
                        builders[slot] = builder;
                    }
                    builder.AddClass(value);
                }
            }

            // 5. Build final map
            return builders.ToDictionary(
                kv => kv.Key,
                kv => merge.Merge(kv.Value.Build()));
        };
    }

    #region Helpers

    private static void AddClassForSlot<TSlots>(
        Dictionary<string, CssBuilder> builders,
        Expression<SlotAccessor<TSlots>> accessor,
        string classes) where TSlots : ISlots
    {
        var name = GetSlot(accessor);
        if (!builders.TryGetValue(name, out var builder))
        {
            builder = new CssBuilder();
            builders[name] = builder;
        }
        builder.AddClass(classes);
    }

    private static Dictionary<string, CssBuilder> ApplyCompoundVariants<TOwner, TSlots>(
        TvOptions<TOwner, TSlots>? options,
        TOwner owner,
        Dictionary<string, CssBuilder> builders)
        where TSlots : ISlots
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
                    AddClassForSlot<TSlots>(builders, s => s.Base, cv.Class);
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
                        AddClassForSlot(builders, slotKv.Key, (string)slotKv.Value);
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

    private static Dictionary<string, CssBuilder> ApplyVariants<TOwner, TSlots>(
        TOwner owner,
        Dictionary<string, CssBuilder> builders,
        IReadOnlyDictionary<string, CompiledVariant<TOwner, TSlots>> baseVariants)
        where TSlots : ISlots
    {
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
                        AddClassForSlot(builders, kv.Key, (string)kv.Value);
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

    private static Dictionary<string, string?> PrecomputeBaseAndTopLevelSlots<TOwner, TSlots>(TvOptions<TOwner, TSlots>? options)
        where TSlots : ISlots
    {
        var map = new Dictionary<string, string?>(StringComparer.Ordinal);

        if (options?.Base is not null)
        {
            map[GetSlot<TSlots>(s => s.Base)] = (string)options.Base;
        }

        if (options?.Slots is not null)
        {
            foreach (var (key, value) in options.Slots)
            {
                if (value is not null)
                {
                    map[GetSlot(key)] = (string)value;
                }
            }
        }

        return map;
    }

    private static Dictionary<string, CompiledVariant<TOwner, TSlots>> PrecomputeVariantDefinitions<TOwner, TSlots>(TvOptions<TOwner, TSlots>? options)
        where TSlots : ISlots
    {
        var variants = new Dictionary<string, CompiledVariant<TOwner, TSlots>>(StringComparer.Ordinal);

        if (options?.Variants is not null)
        {
            foreach (var (key, value) in options.Variants)
            {
                var id = key.ToString() ?? Guid.NewGuid().ToString();
                var accessor = key.Compile();
                variants[id] = new CompiledVariant<TOwner, TSlots>(key, value, accessor);
            }
        }

        return variants;
    }

    private record struct CompiledVariant<TOwner, TSlots>(Expression<VariantAccessor<TOwner>> Expr, IVariant<TSlots> Entry, VariantAccessor<TOwner> Accessor)
        where TSlots : ISlots;

    #endregion Helpers
}