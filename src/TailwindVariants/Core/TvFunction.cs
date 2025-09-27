using System.Diagnostics;
using System.Linq.Expressions;
using static TailwindVariants.TvHelpers;

namespace TailwindVariants;

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
        where TSlots : ISlots
    {
        // Precompute base classes and slot classes as simple strings (immutable)
        var baseClasses = PrecomputeBaseAndTopLevelSlots(options);

        // Precompute compiled variants (compile expression once)
        var baseVariants = PrecomputeVariantDefinitions(options);

        return (owner, merge, overrides) =>
        {
            // Create a per-call set of builders seeded with base classes
            var builders = baseClasses.ToDictionary(
                kv => kv.Key,
                kv => new CssBuilder(kv.Value));

            Dictionary<string, CompiledVariant<TOwner, TSlots>> variants = [];
            if (overrides is not null)
            {
                foreach (var (keyExpr, variant) in overrides)
                {
                    var id = keyExpr.ToString() ?? Guid.NewGuid().ToString();
                    var accessor = keyExpr.Compile();
                    variants[id] = new CompiledVariant<TOwner, TSlots>(keyExpr, variant, accessor);
                }
            }

            // Apply variants using overlay enumeration (overrides take precedence).
            builders = ApplyVariants(owner, builders, baseVariants, variants);

            // Apply compound variants etc. (unchanged)
            builders = ApplyCompoundVariants(options, owner, builders);

            // Build final dictionary and run TailwindMerge's Merge over each class string
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
        IReadOnlyDictionary<string, CompiledVariant<TOwner, TSlots>> baseVariants,
        IReadOnlyDictionary<string, CompiledVariant<TOwner, TSlots>> overrideVariants)
        where TSlots : ISlots
    {
        // Helper to evaluate one compiled variant safely and apply its classes
        void TryApply(CompiledVariant<TOwner, TSlots> compiled)
        {
            var entry = compiled.Entry;
            var accessor = compiled.Accessor;
            try
            {
                var selected = accessor(owner);
                if (selected is null) return;

                if (entry.TryGetSlots(selected, out var slots) && slots is not null)
                {
                    foreach (var kv in slots)
                    {
                        AddClassForSlot(builders, kv.Key, (string)kv.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                // keep library robust — use Debug or logger per your preference
                Debug.WriteLine($"Variant evaluation failed for '{compiled.Expr}': {ex.Message}");
            }
        }

        // 1) apply all override variants first (they win)
        if (overrideVariants is not null && overrideVariants.Count > 0)
        {
            foreach (var compiled in overrideVariants.Values)
                TryApply(compiled);
        }

        // 2) apply base variants that weren't overridden
        foreach (var kv in baseVariants)
        {
            if (overrideVariants != null && overrideVariants.ContainsKey(kv.Key))
                continue;

            TryApply(kv.Value);
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
