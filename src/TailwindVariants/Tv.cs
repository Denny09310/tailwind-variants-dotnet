using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using TailwindMerge;

namespace TailwindVariants;

/// <summary>
/// Delegate that represents a function used to compute final Tailwind CSS classes
/// for a component, given a <see cref="TwMerge"/> instance and optional overrides.
/// </summary>
/// <typeparam name="TOwner">The component or owner type that defines variants and slots.</typeparam>
/// <typeparam name="TSlots">The slot container type used for organizing classes.</typeparam>
/// <param name="twMerge">The TailwindMerge instance responsible for conflict resolution and merging.</param>
/// <param name="overrides">Optional overrides applied on top of the base configuration.</param>
/// <returns>The merged Tailwind CSS classes string, or <c>null</c> if nothing applies.</returns>
public delegate string? TvOverrides<TOwner, TSlots>(TwMerge twMerge, TvConfig<TOwner, TSlots>? overrides = null)
    where TSlots : ISlots, new();

/// <summary>
/// Defines a contract for objects that expose typed slot definitions.
/// </summary>
/// <typeparam name="TSlots">The slot type implementing <see cref="ISlots"/>.</typeparam>
public interface IHasSlots<TSlots> where TSlots : ISlots
{
    /// <summary>
    /// Gets the configured classes for each slot.
    /// </summary>
    TSlots? Classes { get; }
}

/// <summary>
/// Marker interface for slot containers. Provides a base slot property.
/// </summary>
public interface ISlots
{
    /// <summary>
    /// Gets or sets the CSS classes applied to the base slot.
    /// </summary>
    string? Base { get; set; }
}

public static class Tv
{
    public static Tv<TOwner, TSlots> Create<TOwner, TSlots>(TOwner owner, TvConfig<TOwner, TSlots> config)
       where TSlots : ISlots, new() => new(owner, config);
}

/// <summary>
/// Represents a slot-only mapping that can be used with object initializers.
/// Enables syntax such as <c>new() { [s =&gt; s.Icon] = "mr-2" }</c>.
/// </summary>
/// <typeparam name="TSlots">The slot container type.</typeparam>
public class SlotMap<TSlots> where TSlots : new()
{
    private readonly Dictionary<string, string?> _slots = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Indexer that maps a slot property selector to a CSS class string.
    /// </summary>
    /// <param name="prop">The slot property expression (e.g., <c>s =&gt; s.Icon</c>).</param>
    public object? this[Expression<Func<TSlots, string?>> prop]
    {
        set
        {
            var name = GetPropName(prop);
            if (value is string s)
            {
                _slots[name] = s;
            }
            else
            {
                throw new ArgumentException("SlotMap assignment must be string");
            }
        }
    }

    /// <summary>
    /// Creates a new <typeparamref name="TSlots"/> instance and applies all recorded assignments.
    /// </summary>
    public TSlots ToSlots()
    {
        var slots = new TSlots();
        foreach (var kv in _slots)
        {
            var prop = typeof(TSlots).GetProperty(kv.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (prop != null && prop.CanWrite && prop.PropertyType == typeof(string))
            {
                prop.SetValue(slots, kv.Value);
            }
        }
        return slots;
    }

    private static string GetPropName(Expression<Func<TSlots, string?>> expr)
    {
        if (expr.Body is MemberExpression m)
        {
            return m.Member.Name;
        }

        if (expr.Body is UnaryExpression u && u.Operand is MemberExpression m2)
        {
            return m2.Member.Name;
        }

        throw new ArgumentException("Slot accessor must be simple member access, e.g. s => s.Icon");
    }
}

/// <summary>
/// Main entry point for evaluating Tailwind variants.
/// Handles merging of base classes, variant groups, slot overrides, and compound variants.
/// </summary>
/// <typeparam name="TOwner">The owner type (component) that defines variants.</typeparam>
/// <typeparam name="TSlots">The slot container type.</typeparam>
public class Tv<TOwner, TSlots> where TSlots : ISlots, new()
{
    private readonly TvConfig<TOwner, TSlots> _config;
    private readonly TOwner _owner;

    internal Tv(TOwner owner, TvConfig<TOwner, TSlots> config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    /// <summary>
    /// Computes the final classes for a specific slot by merging defaults,
    /// variant values, overrides, and compound variants.
    /// </summary>
    /// <param name="accessor">Expression selecting the slot property (e.g. <c>s =&gt; s.Icon</c>).</param>
    /// <param name="twMerge">The TailwindMerge instance to apply conflict resolution.</param>
    /// <param name="overrides">Optional configuration overrides.</param>
    /// <returns>The merged CSS class string for the slot, or <c>null</c> if no classes apply.</returns>
    public TvOverrides<TOwner, TSlots> Build()
    {
        string? Compute(TwMerge twMerge, TvConfig<TOwner, TSlots>? overrides = null)
        {
            ArgumentNullException.ThrowIfNull(twMerge);

            var merged = new List<string?>();

            // base / override base
            if (!string.IsNullOrWhiteSpace(_config.Base))
            {
                merged.Add(_config.Base);
            }

            if (!string.IsNullOrWhiteSpace(overrides?.Base))
            {
                merged.Add(overrides.Base);
            }

            // variant groups order: config.Variants (in insertion order) then overrides.Variants appended
            var groups = _config.Variants.ToList();
            if (overrides?.Variants != null)
            {
                groups.AddRange(overrides.Variants);
            }

            // gather classes from groups
            foreach (var g in groups)
            {
                var c = g.GetClasses(_owner);
                if (!string.IsNullOrWhiteSpace(c))
                {
                    merged.Add(c);
                }
            }

            // compound variants (apply classes)
            var compounds = _config.CompoundVariants.Concat(overrides?.CompoundVariants ?? []);
            foreach (var cv in compounds)
            {
                if (cv.Predicate(_owner))
                {
                    if (!string.IsNullOrWhiteSpace(cv.Overrides?.Base))
                    {
                        merged.Add(cv.Overrides.Base);
                    }
                }
            }

            return twMerge.Merge([.. merged.Where(x => !string.IsNullOrWhiteSpace(x))]);
        }

        return Compute;
    }

    /// <summary>
    /// Builds a <see cref="TvOverrides{TOwner,TSlots}"/> delegate that, when invoked,
    /// computes the final CSS classes for the owner.
    /// </summary>
    public string? GetSlot(Expression<Func<TSlots, string?>> accessor, TwMerge twMerge, TvConfig<TOwner, TSlots>? overrides = null)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        ArgumentNullException.ThrowIfNull(twMerge);

        var propName = GetPropertyNameFromExpression(accessor);
        var bucket = new List<string?>();

        // base slot classes from config
        var baseSlot = GetSlotProp(_config.Slots, propName);
        if (!string.IsNullOrWhiteSpace(baseSlot))
        {
            bucket.Add(baseSlot);
        }

        // variant groups slot overrides (in order)
        foreach (var g in _config.Variants)
        {
            var dto = g.GetSlotOverrides(_owner);
            if (dto != null)
            {
                var v = GetSlotProp(dto, propName);
                if (!string.IsNullOrWhiteSpace(v))
                {
                    bucket.Add(v);
                }
            }
        }

        // overrides' slots
        if (overrides is not null && overrides.Slots != null)
        {
            var v = GetSlotProp(overrides.Slots, propName);
            if (!string.IsNullOrWhiteSpace(v))
            {
                bucket.Add(v);
            }
        }

        // compound variants slot overrides
        foreach (var cv in _config.CompoundVariants)
        {
            if (cv.Predicate(_owner) && cv.Overrides != null)
            {
                var v = GetSlotProp(cv.Overrides, propName);
                if (!string.IsNullOrWhiteSpace(v))
                {
                    bucket.Add(v);
                }
            }
        }

        // final merge
        return twMerge.Merge([.. bucket.Where(x => !string.IsNullOrWhiteSpace(x))]);
    }

    private static string GetPropertyNameFromExpression(Expression<Func<TSlots, string?>> expr)
    {
        if (expr.Body is MemberExpression m)
        {
            return m.Member.Name;
        }

        if (expr.Body is UnaryExpression u && u.Operand is MemberExpression m2)
        {
            return m2.Member.Name;
        }

        throw new ArgumentException("Slot accessor must be simple member access, e.g. s => s.Icon");
    }

    private static string? GetSlotProp(TSlots slotsObj, string propName)
    {
        var prop = typeof(TSlots).GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        return prop?.GetValue(slotsObj) as string;
    }
}

/// <summary>
/// Configuration for a <see cref="Tv{TOwner, TSlots}"/> instance.
/// Holds base classes, slots, variant groups, and compound variants.
/// </summary>
/// <typeparam name="TOwner">The owner (component) type.</typeparam>
/// <typeparam name="TSlots">The slot container type.</typeparam>
public class TvConfig<TOwner, TSlots>
    where TSlots : ISlots, new()
{
    /// <summary>Gets or sets the base CSS classes for the component.</summary>
    public string? Base { get; set; }

    /// <summary>Gets or sets the collection of compound variants.</summary>
    public List<CompoundVariant<TOwner, TSlots>> CompoundVariants { get; set; } = [];

    /// <summary>Gets or sets the slot defaults.</summary>
    public TSlots Slots { get; set; } = new();

    /// <summary>Gets or sets the collection of variant groups.</summary>
    public VariantGroupCollection<TOwner, TSlots> Variants { get; set; } = [];
}

/// <summary>
/// Represents a mapping between a variant accessor (e.g. <c>b =&gt; b.Variant</c>)
/// and its possible values (classes or slot overrides).
/// </summary>
/// <typeparam name="TOwner">The owner type.</typeparam>
/// <typeparam name="TSlots">The slot container type.</typeparam>
public class VariantGroup<TOwner, TSlots>(Expression<Func<TOwner, object?>> accessor)
    where TSlots : new()
{
    private readonly Dictionary<object, VariantValue<TSlots>> _map = new(ObjectComparer.Instance);

    /// <summary>
    /// Gets the accessor expression used to evaluate the current variant.
    /// </summary>
    public Expression<Func<TOwner, object?>> Accessor { get; } = accessor ?? throw new ArgumentNullException(nameof(accessor));

    /// <summary>
    /// Assigns classes or slot overrides to a specific variant key (enum, bool, etc.).
    /// Supports string, <see cref="SlotMap{TSlots}"/>, or <see cref="VariantValue{TSlots}"/>.
    /// </summary>
    public object? this[object key]
    {
        set
        {
            // if RHS is SlotMap<TSlots>, we attach slots to that key
            if (!_map.TryGetValue(NormalizeKey(key), out var entry))
            {
                entry = new VariantValue<TSlots>();
                _map[NormalizeKey(key)] = entry;
            }

            switch (value)
            {
                case null:
                    entry.Classes = null;
                    break;

                case string s:
                    entry.Classes = s;
                    break;

                case SlotMap<TSlots> slotMap:
                    entry.Slots = slotMap.ToSlots();
                    break;

                case VariantValue<TSlots> vv:
                    // allow setting VariantValue directly (rare)
                    _map[NormalizeKey(key)] = vv;
                    break;

                default:
                    // if value is an object initializer "new() { [s => s.Icon] = "x" }", it will be SlotMap<TSlots>
                    throw new ArgumentException($"Unsupported assignment value type: {value.GetType().Name}");
            }
        }
    }

    /// <summary>
    /// Gets the CSS classes associated with the owner’s evaluated variant value.
    /// </summary>
    public string? GetClasses(TOwner owner)
    {
        var key = EvaluateAccessor(owner);
        if (key == null)
        {
            return null;
        }

        if (_map.TryGetValue(NormalizeKey(key), out var v))
        {
            return v?.Classes;
        }

        return null;
    }

    /// <summary>
    /// Gets the slot overrides associated with the owner’s evaluated variant value.
    /// </summary>
    public TSlots? GetSlotOverrides(TOwner owner)
    {
        var key = EvaluateAccessor(owner);
        if (key == null)
        {
            return default;
        }

        if (_map.TryGetValue(NormalizeKey(key), out var v))
        {
            return v.Slots;
        }

        return default;
    }

    private static object NormalizeKey(object? k)
    {
        if (k == null)
        {
            return NullKey.Value;
        }

        if (k is Enum e)
        {
            return e; // keep Enum boxed
        }

        if (k is bool b)
        {
            return b;
        }
        // allow numeric constants, strings etc
        return k;
    }

    private object? EvaluateAccessor(TOwner owner)
    {
        var func = Accessor.Compile();
        return func(owner);
    }

    private sealed class NullKey
    {
        public static readonly NullKey Value = new();
    }

    // internal comparer to handle boxed enums as keys
    private class ObjectComparer : IEqualityComparer<object>
    {
        public static readonly ObjectComparer Instance = new();

        public new bool Equals(object? x, object? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }
            // if both enums, compare by underlying value & type
            if (x is Enum ex && y is Enum ey)
            {
                return ex.GetType() == ey.GetType() && ex.Equals(ey);
            }

            return x.Equals(y);
        }

        public int GetHashCode(object obj)
        {
            if (obj is Enum e)
            {
                return HashCode.Combine(e.GetType(), e);
            }

            return obj?.GetHashCode() ?? 0;
        }
    }
}

/// <summary>
/// Collection wrapper for <see cref="VariantGroup{TOwner, TSlots}"/>.
/// Supports collection initializers and enumeration.
/// </summary>
public class VariantGroupCollection<TOwner, TSlots> : IEnumerable<VariantGroup<TOwner, TSlots>>
    where TSlots : new()
{
    private readonly List<VariantGroup<TOwner, TSlots>> _inner = [];

    // Add for collection initializer
    public void Add(VariantGroup<TOwner, TSlots> group) => _inner.Add(group);

    public IEnumerator<VariantGroup<TOwner, TSlots>> GetEnumerator() => _inner.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();

    // expose ToList convenience
    public List<VariantGroup<TOwner, TSlots>> ToList() => [.. _inner];
}

/// <summary>
/// Represents the value associated with a variant key:
/// optional classes and optional slot overrides.
/// </summary>
public class VariantValue<TSlots> where TSlots : new()
{
    public string? Classes { get; set; }
    public TSlots? Slots { get; set; }
}

/// <summary>
/// Represents a compound variant, which activates when a predicate matches.
/// Can contribute either classes, slot overrides, or both.
/// </summary>
/// <typeparam name="TOwner">The owner type.</typeparam>
/// <typeparam name="TSlots">The slot container type.</typeparam>
public class CompoundVariant<TOwner, TSlots>(Expression<Func<TOwner, bool>> predicate, SlotMap<TSlots> slotMap)
    where TSlots : ISlots, new()
{
    public TSlots? Overrides { get; init; } = slotMap.ToSlots();
    public Func<TOwner, bool> Predicate { get; init; } = predicate.Compile();
}