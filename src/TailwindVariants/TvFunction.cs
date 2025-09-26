using BlazorComponentUtilities;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using Tw = TailwindMerge.TwMerge;

namespace TailwindVariants;

public delegate string? SlotAccessor<TSlots>(TSlots slots) where TSlots : ISlots;

public delegate SlotMap<TSlots> TvReturnType<TOwner, TSlots>(TOwner owner, Tw merge, VariantCollection<TOwner, TSlots>? overrides = null)
    where TSlots : ISlots;

public delegate object? VariantAccessor<TOwner>(TOwner owner);

public interface ISlots
{
    string? Base { get; }
}

public interface IVariant<TSlots> where TSlots : ISlots
{
    bool TryGetSlots<TKey>(TKey key, [MaybeNullWhen(false)] out SlotCollection<TSlots> slots);
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

public class CompoundVariant<TOwner, TSlots>(Predicate<TOwner> predicate) : IEnumerable<KeyValuePair<Expression<VariantAccessor<TSlots>>, SlotCollection<TSlots>>>
    where TSlots : ISlots
{
    private Dictionary<Expression<VariantAccessor<TSlots>>, SlotCollection<TSlots>>? _values;

    public string? Class { get; set; }
    internal Predicate<TOwner> Predicate { get; } = predicate;

    public SlotCollection<TSlots> this[Expression<VariantAccessor<TSlots>> key]
    {
        get => _values?[key] ?? throw new InvalidOperationException();
        set => _values?[key] = value;
    }

    public void Add(Expression<VariantAccessor<TSlots>> key, SlotCollection<TSlots> value)
    {
        _values ??= [];
        _values.Add(key, value);
    }

    public IEnumerator<KeyValuePair<Expression<VariantAccessor<TSlots>>, SlotCollection<TSlots>>> GetEnumerator()
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

public class SlotCollection<TSlots>() : IEnumerable<KeyValuePair<Expression<SlotAccessor<TSlots>>, ClassValue>>
    where TSlots : ISlots
{
    private readonly Dictionary<Expression<SlotAccessor<TSlots>>, ClassValue> _slots = [];

    internal SlotCollection(string classes) : this() => _slots[b => b.Base] = classes;

    public ClassValue this[Expression<SlotAccessor<TSlots>> key]
    {
        get => _slots[key];
        set => _slots[key] = value;
    }

    public static implicit operator SlotCollection<TSlots>(string classes) => new(classes);

    public void Add(Expression<SlotAccessor<TSlots>> key, ClassValue value) => _slots.Add(key, value);

    public IEnumerator<KeyValuePair<Expression<SlotAccessor<TSlots>>, ClassValue>> GetEnumerator() => _slots.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class SlotMap<TSlots> where TSlots : ISlots
{
    private readonly Dictionary<string, string?> _map = [];

    public string? this[Expression<SlotAccessor<TSlots>> key]
    {
        get => _map.TryGetValue(GetSlot(key), out var value) ? value : null;
        set => _map[GetSlot(key)] = value ?? throw new ArgumentNullException(nameof(value));
    }

    public static implicit operator SlotMap<TSlots>(Dictionary<string, string?> map)
    {
        var slots = new SlotMap<TSlots>();
        foreach (var (key, value) in map)
        {
            slots.Add(key, value);
        }
        return slots;
    }

    public void Add(string key, string? value) => _map.Add(key, value);

    private static string GetSlot(Expression<SlotAccessor<TSlots>> accessor)
    {
        if (accessor.Body is MemberExpression memberExpr)
        {
            return memberExpr.Member.Name;
        }
        throw new ArgumentException("Invalid slot accessor expression", nameof(accessor));
    }
}

public class TvOptions<TOwner, TSlots>
    where TSlots : ISlots
{
    public ClassValue? Base { get; set; }
    public CompoundVariantCollection<TOwner, TSlots>? CompoundVariants { get; set; }
    public SlotCollection<TSlots>? Slots { get; set; }
    public VariantCollection<TOwner, TSlots>? Variants { get; set; }
}

public class Variant<TVariant, TSlots> : IVariant<TSlots>,
    IEnumerable<KeyValuePair<TVariant, SlotCollection<TSlots>>>
    where TVariant : notnull
    where TSlots : ISlots
{
    private readonly Dictionary<TVariant, SlotCollection<TSlots>> _variants = [];

    public SlotCollection<TSlots> this[TVariant key]
    {
        get => _variants[key];
        set => _variants[key] = value;
    }

    public void Add(TVariant key, SlotCollection<TSlots> value) => _variants.Add(key, value);

    public IEnumerator<KeyValuePair<TVariant, SlotCollection<TSlots>>> GetEnumerator() => _variants.GetEnumerator();

    public bool TryGetSlots<TKey>(TKey key, [MaybeNullWhen(false)] out SlotCollection<TSlots> slots)
    {
        if (key is TVariant v && _variants.TryGetValue(v, out slots))
        {
            return true;
        }

        slots = null;
        return false;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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
        // 1) base classes (from options)
        var builders = PrecomputeBaseAndTopLevelSlots(options);

        // 2) Precompute variants definitions
        var variants = PrecomputeVariantDefinitions(options);

        return (owner, merge, overrides) =>
        {
            // 3) combine variant overridden by `overrides` (call-time overrides)
            variants = MergeVariantDefinitions(variants, overrides);

            // 4) for each active variant, evaluate the accessor and add corresponding slot classes
            builders = ApplyVariants(owner, builders, variants);

            // 5) compound variants from options
            builders = ApplyCompoundVariants(options, owner, builders);

            // Final string (Tailwind merge not applied here; plug merge.Merge(...) if desired)
            return builders.ToDictionary(
                kv => kv.Key,
                kv => merge.Merge(kv.Value.Build()));
        };
    }

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
        builders[name] = builder.AddClass(classes);
    }

    private static Dictionary<string, CssBuilder> ApplyCompoundVariants<TOwner, TSlots>(
        TvOptions<TOwner, TSlots> options,
        TOwner owner,
        Dictionary<string, CssBuilder> builders)
        where TSlots : ISlots
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
                            AddClassForSlot<TSlots>(builders, s => s.Base, cv.Class);
                        }
                    }
                }
                catch
                {
                    // ignore predicate errors
                }
            }
        }

        return builders;
    }

    private static Dictionary<string, CssBuilder> ApplyVariants<TOwner, TSlots>(
        TOwner owner,
        Dictionary<string, CssBuilder> builders,
        Dictionary<string, CompiledVariant<TOwner, TSlots>> variants)
        where TSlots : ISlots
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
                        AddClassForSlot(builders, kv2.Key, (string)kv2.Value);
                    }
                }
            }
            catch
            {
                // swallow per-variant errors to remain robust
                continue;
            }
        }

        return builders;
    }

    private static string GetSlot<TSlots>(Expression<SlotAccessor<TSlots>> accessor) where TSlots : ISlots
    {
        if (accessor.Body is MemberExpression memberExpr)
        {
            return memberExpr.Member.Name;
        }
        throw new ArgumentException("Invalid slot accessor expression", nameof(accessor));
    }

    private static Dictionary<string, CompiledVariant<TOwner, TSlots>> MergeVariantDefinitions<TOwner, TSlots>(
        Dictionary<string, CompiledVariant<TOwner, TSlots>> variants,
        VariantCollection<TOwner, TSlots>? overrides) where TSlots : ISlots
    {
        if (overrides is not null)
        {
            foreach (var (key, value) in overrides)
            {
                var id = key.ToString() ?? Guid.NewGuid().ToString();
                var accessor = key.Compile();
                variants[id] = new(key, value, accessor);
            }
        }

        return variants;
    }

    private static Dictionary<string, CssBuilder> PrecomputeBaseAndTopLevelSlots<TOwner, TSlots>(TvOptions<TOwner, TSlots> options)
        where TSlots : ISlots
    {
        var builders = new Dictionary<string, CssBuilder>();

        if (options?.Base is not null)
        {
            AddClassForSlot<TSlots>(builders, s => s.Base, (string)options.Base);
        }

        if (options?.Slots is not null)
        {
            foreach (var (key, value) in options.Slots)
            {
                if (value is not null)
                {
                    AddClassForSlot(builders, key, (string)value);
                }
            }
        }

        return builders;
    }

    private static Dictionary<string, CompiledVariant<TOwner, TSlots>> PrecomputeVariantDefinitions<TOwner, TSlots>(TvOptions<TOwner, TSlots> options) where TSlots : ISlots
    {
        var variants = new Dictionary<string, CompiledVariant<TOwner, TSlots>>();

        if (options?.Variants is not null)
        {
            foreach (var (key, value) in options.Variants)
            {
                var id = key.ToString() ?? Guid.NewGuid().ToString();
                var accessor = key.Compile();
                variants[id] = new(key, value, accessor);
            }
        }

        return variants;
    }

    private record struct CompiledVariant<TOwner, TSlots>(Expression<VariantAccessor<TOwner>> Expr, IVariant<TSlots> Entry, VariantAccessor<TOwner> Accessor) where TSlots : ISlots;
}