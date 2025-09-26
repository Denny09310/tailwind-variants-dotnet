using BlazorComponentUtilities;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using Tw = TailwindMerge.TwMerge;

namespace TailwindVariants;

public delegate string? SlotAccessor<TSlots>(TSlots slots) where TSlots : ISlots;

public delegate string? TvReturnType<TOwner, TSlots>(TOwner owner, Tw merge, VariantCollection<TOwner, TSlots>? overrides = null)
    where TSlots : ISlots;

public delegate object? VariantAccessor<TOwner>(TOwner owner);

public interface ISlots
{
    string? Base { get; }
}

public interface IVariant<TSlots> where TSlots : ISlots
{
    bool TryGetSlots<TKey>(TKey key, [MaybeNullWhen(false)] out SlotsCollection<TSlots> slots);
}

public class ClassValue() : IEnumerable<string>
{
    private readonly string? _value;
    private ICollection<string>? _values;

    internal ClassValue(string value) : this()
    {
        _value = value;
    }

    public static implicit operator ClassValue(string value) => new(value);

    public static implicit operator string(ClassValue @class)
    {
        if (!string.IsNullOrEmpty(@class._value))
        {
            return @class._value!;
        }

        if (@class._values is not null)
        {
            return string.Join(" ", @class._values);
        }

        throw new InvalidOperationException("No class value present.");
    }

    public void Add(string value)
    {
        _values ??= [];
        _values.Add(value);
    }

    public IEnumerator<string> GetEnumerator() => _values?.GetEnumerator() ?? throw new InvalidOperationException();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class CompoundVariant<TOwner, TSlots>(Predicate<TOwner> predicate) : IEnumerable<KeyValuePair<Expression<VariantAccessor<TSlots>>, SlotsCollection<TSlots>>>
    where TSlots : ISlots
{
    private Dictionary<Expression<VariantAccessor<TSlots>>, SlotsCollection<TSlots>>? _values;

    public string? Class { get; set; }
    internal Predicate<TOwner> Predicate { get; } = predicate;

    public SlotsCollection<TSlots> this[Expression<VariantAccessor<TSlots>> key]
    {
        get => _values?[key] ?? throw new InvalidOperationException();
        set => _values?[key] = value;
    }

    public void Add(Expression<VariantAccessor<TSlots>> key, SlotsCollection<TSlots> value)
    {
        _values ??= [];
        _values.Add(key, value);
    }

    public IEnumerator<KeyValuePair<Expression<VariantAccessor<TSlots>>, SlotsCollection<TSlots>>> GetEnumerator()
        => _values?.GetEnumerator() ?? throw new InvalidOperationException();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class CompoundVariantCollection<TOwner, TSlots> : IEnumerable<CompoundVariant<TOwner, TSlots>>
    where TSlots : ISlots
{
    private readonly ICollection<CompoundVariant<TOwner, TSlots>> _variants = [];

    public void Add(CompoundVariant<TOwner, TSlots> entry) => _variants.Add(entry);

    public IEnumerator<CompoundVariant<TOwner, TSlots>> GetEnumerator() => _variants.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class SlotsCollection<TSlots>() : IEnumerable<KeyValuePair<Expression<SlotAccessor<TSlots>>, ClassValue>>
    where TSlots : ISlots
{
    private readonly Dictionary<Expression<SlotAccessor<TSlots>>, ClassValue> _slots = [];

    internal SlotsCollection(string classes) : this() => _slots[b => b.Base] = classes;

    public ClassValue this[Expression<SlotAccessor<TSlots>> key]
    {
        get => _slots[key];
        set => _slots[key] = value;
    }

    public static implicit operator SlotsCollection<TSlots>(string classes) => new(classes);

    public void Add(Expression<SlotAccessor<TSlots>> key, ClassValue value) => _slots.Add(key, value);

    public IEnumerator<KeyValuePair<Expression<SlotAccessor<TSlots>>, ClassValue>> GetEnumerator() => _slots.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class TvOptions<TOwner, TSlots>
    where TSlots : ISlots
{
    public ClassValue? Base { get; set; }
    public CompoundVariantCollection<TOwner, TSlots>? CompoundVariants { get; set; }
    public SlotsCollection<TSlots>? Slots { get; set; }
    public VariantCollection<TOwner, TSlots>? Variants { get; set; }
}

public class Variant<TVariant, TSlots> : IVariant<TSlots>,
    IEnumerable<KeyValuePair<TVariant, SlotsCollection<TSlots>>>
    where TVariant : notnull
    where TSlots : ISlots
{
    private readonly Dictionary<TVariant, SlotsCollection<TSlots>> _variants = [];

    public SlotsCollection<TSlots> this[TVariant key]
    {
        get => _variants[key];
        set => _variants[key] = value;
    }

    public void Add(TVariant key, SlotsCollection<TSlots> value) => _variants.Add(key, value);

    public IEnumerator<KeyValuePair<TVariant, SlotsCollection<TSlots>>> GetEnumerator() => _variants.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool TryGetSlots<TKey>(TKey key, [MaybeNullWhen(false)] out SlotsCollection<TSlots> slots)
    {
        if (key is TVariant v && _variants.TryGetValue(v, out slots))
        {
            return true;
        }

        slots = null;
        return false;
    }
}

public class VariantCollection<TOwner, TSlots> : IEnumerable<KeyValuePair<Expression<VariantAccessor<TOwner>>, IVariant<TSlots>>>
    where TSlots : ISlots
{
    private readonly Dictionary<Expression<VariantAccessor<TOwner>>, IVariant<TSlots>> _variants = [];

    public IVariant<TSlots> this[Expression<VariantAccessor<TOwner>> key]
    {
        get => _variants[key];
        set => _variants[key] = value;
    }

    public void Add(Expression<VariantAccessor<TOwner>> key, IVariant<TSlots> value) => _variants.Add(key, value);

    public IEnumerator<KeyValuePair<Expression<VariantAccessor<TOwner>>, IVariant<TSlots>>> GetEnumerator() => _variants.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public static class TvFunction
{
    public static TvReturnType<TOwner, TSlots> Tv<TOwner, TSlots>(TvOptions<TOwner, TSlots> options)
        where TSlots : ISlots
    {
        return (owner, merge, overrides) =>
        {
            var builder = new CssBuilder();

            // 1) base classes (from options)
            builder = AddBaseAndTopLevelSlots(options, builder);

            // 2) combine variant definitions: options.Variants, overridden by `overrides` (call-time overrides)
            var variants = MergeVariantDefinitions(options, overrides);

            // 3) for each active variant, evaluate the accessor and add corresponding slot classes
            builder = ApplyVariants(owner, builder, variants);

            // 4) compound variants from options
            builder = ApplyCompoundVariants(options, owner, builder);

            // Final string (Tailwind merge not applied here; plug merge.Merge(...) if desired)
            return builder.Build();
        };
    }

    private static CssBuilder AddBaseAndTopLevelSlots<TOwner, TSlots>(TvOptions<TOwner, TSlots> options, CssBuilder builder) where TSlots : ISlots
    {
        if (options?.Base is not null)
        {
            builder.AddClass((string)options.Base);
        }

        // top-level slots from options (if any) — add all defined classes
        if (options?.Slots is not null)
        {
            foreach (var kv in options.Slots)
            {
                var cv = kv.Value;
                if (cv is not null)
                {
                    builder.AddClass((string)cv);
                }
            }
        }

        return builder;
    }

    private static CssBuilder ApplyCompoundVariants<TOwner, TSlots>(TvOptions<TOwner, TSlots> options, TOwner owner, CssBuilder builder) where TSlots : ISlots
    {
        if (options?.CompoundVariants is not null)
        {
            foreach (var cv in options.CompoundVariants)
            {
                try
                {
                    if (cv.Predicate(owner))
                    {
                        if (!string.IsNullOrEmpty(cv.Class))
                        {
                            builder.AddClass(cv.Class);
                        }
                    }
                }
                catch
                {
                    // ignore predicate errors
                }
            }
        }

        return builder;
    }

    private static CssBuilder ApplyVariants<TOwner, TSlots>(TOwner owner, CssBuilder builder, Dictionary<string, CompiledVariant<TOwner, TSlots>> variants) where TSlots : ISlots
    {
        foreach (var pair in variants.Values)
        {
            var entry = pair.Entry;
            var accessor = pair.Accessor;

            try
            {
                var selected = accessor(owner);
                if (selected is null)
                {
                    continue;
                }

                if (entry.TryGetSlots(selected, out var slots) && slots is not null)
                {
                    foreach (var kv2 in slots)
                    {
                        builder.AddClass((string)kv2.Value);
                    }
                }
            }
            catch
            {
                // swallow per-variant errors to remain robust
                continue;
            }
        }

        return builder;
    }

    private static Dictionary<string, CompiledVariant<TOwner, TSlots>> MergeVariantDefinitions<TOwner, TSlots>(TvOptions<TOwner, TSlots> options, VariantCollection<TOwner, TSlots>? overrides) where TSlots : ISlots
    {
        var variants = new Dictionary<string, CompiledVariant<TOwner, TSlots>>();

        if (options?.Variants is not null)
        {
            foreach (var kv in options.Variants)
            {
                var keyStr = kv.Key.ToString() ?? Guid.NewGuid().ToString();
                var accessor = kv.Key.Compile();
                variants[keyStr] = new(kv.Key, kv.Value, accessor);
            }
        }

        if (overrides is not null)
        {
            foreach (var kv in overrides)
            {
                var keyStr = kv.Key.ToString() ?? Guid.NewGuid().ToString();
                var accessor = kv.Key.Compile();
                variants[keyStr] = new(kv.Key, kv.Value, accessor);
            }
        }

        return variants;
    }

    private record struct CompiledVariant<TOwner, TSlots>(Expression<VariantAccessor<TOwner>> Expr, IVariant<TSlots> Entry, VariantAccessor<TOwner> Accessor) where TSlots : ISlots;
}