using static TailwindVariants.NET.TvHelpers;

namespace TailwindVariants.NET;

public interface ITwDescriptor
{
    /// <summary>
    /// Compound-variant evaluators in the same shape (optional — keep if you need cross-type compound merging).
    /// </summary>
    IReadOnlyList<Func<object, IReadOnlyDictionary<string, string?>?>>? CompoundVariants { get; }

    /// <summary>Extends pointer (can be another descriptor of any generic shape).</summary>
    ITwDescriptor? Extends { get; }

    /// <summary>Precomputed base/top-level slots as slot-name -> class-string.</summary>
    IReadOnlyDictionary<string, string?>? Slots { get; }

    /// <summary>
    /// Precomputed variant evaluators: id -> evaluator(owner) => slotName -> class-string (or null).
    /// The evaluator MUST be robust: return null / empty if the owner is not the expected type.
    /// </summary>
    IReadOnlyDictionary<string, Func<object, IReadOnlyDictionary<string, string?>?>>? Variants { get; }
}

/// <summary>
/// Represents the configuration options for TailwindVariants, including base classes, slots, variants, and compound variants.
/// </summary>
/// <typeparam name="TOwner">The type that owns the slots and variants.</typeparam>
/// <typeparam name="TSlots">The type representing the slots, which must implement <see cref="ISlots"/>.</typeparam>
public sealed class TvDescriptor<TOwner, TSlots> : ITwDescriptor
    where TSlots : ISlots, new()
    where TOwner : ISlotted<TSlots>
{
    private List<Func<object, IReadOnlyDictionary<string, string?>?>>? _compoundVariants;

    // Untyped adapters used to support heterogeneous extends chains via ITwDescriptor
    private Dictionary<string, Func<object, IReadOnlyDictionary<string, string?>?>>? _variants;

    /// <summary>
    /// Initializes a new instance of the <see cref="TvDescriptor{TOwner, TSlots}"/> class.
    /// </summary>
    /// <param name="base">The base CSS classes to apply to the base slot.</param>
    /// <param name="slots">A collection mapping slot accessors to their corresponding CSS class values.</param>
    /// <param name="variants">A collection of variant definitions, each keyed by an accessor expression.</param>
    /// <param name="compoundVariants">A collection of compound variants, which apply additional classes based on specific predicates.</param>
    public TvDescriptor(
        ITwDescriptor? extends = default,
        ClassValue? @base = null,
        SlotCollection<TSlots>? slots = null,
        VariantCollection<TOwner, TSlots>? variants = null,
        CompoundVariantCollection<TOwner, TSlots>? compoundVariants = null)
    {
        Extends = extends;
        Base = @base;
        Slots = slots;
        Variants = variants;
        CompoundVariants = compoundVariants;

        Precompute();
    }

    /// <summary>
    /// The base CSS classes to apply to the base slot.
    /// </summary>
    public ClassValue? Base { get; }

    /// <summary>
    /// A collection of compound variants, which apply additional classes based on specific predicates.
    /// </summary>
    public CompoundVariantCollection<TOwner, TSlots>? CompoundVariants { get; }

    public ITwDescriptor? Extends { get; set; }

    /// <summary>
    /// A collection mapping slot accessors to their corresponding CSS class values.
    /// </summary>
    public SlotCollection<TSlots>? Slots { get; }

    /// <summary>
    /// A collection of variant definitions, each keyed by an accessor expression.
    /// </summary>
    public VariantCollection<TOwner, TSlots>? Variants { get; }

    internal IReadOnlyDictionary<string, string?> BaseSlots { get; private set; } = default!;

    internal IReadOnlyDictionary<string, CompiledVariant<TOwner, TSlots>> BaseVariants { get; private set; } = default!;

    #region Explicit Implementations

    IReadOnlyList<Func<object, IReadOnlyDictionary<string, string?>?>>? ITwDescriptor.CompoundVariants => _compoundVariants?.AsReadOnly();
    IReadOnlyDictionary<string, string?>? ITwDescriptor.Slots => BaseSlots;
    IReadOnlyDictionary<string, Func<object, IReadOnlyDictionary<string, string?>?>>? ITwDescriptor.Variants => _variants;

    #endregion Explicit Implementations

    private void BuildUntypedAdapters()
    {
        _variants = new Dictionary<string, Func<object, IReadOnlyDictionary<string, string?>?>>(StringComparer.Ordinal);
        _compoundVariants = [];

        // Build untyped evaluators from typed Variants
        if (Variants is not null)
        {
            foreach (var (key, value) in Variants)
            {
                var id = key.ToString() ?? Guid.NewGuid().ToString();
                var accessor = key.Compile(); // Func<TOwner, TSelected?>

                _variants[id] = obj =>
                {
                    if (obj is not TOwner owner) return null;
                    try
                    {
                        var selected = accessor(owner);
                        if (!value.TryGetSlots(selected, out var slots) || slots is null) return null;

                        var map = new Dictionary<string, string?>(StringComparer.Ordinal);
                        foreach (var (slotExpr, classVal) in slots)
                        {
                            if (classVal is null) continue;
                            map[GetSlot(slotExpr)] = (string)classVal;
                        }

                        return map;
                    }
                    catch
                    {
                        return null;
                    }
                };
            }
        }

        // Build untyped evaluators from typed CompoundVariants
        if (CompoundVariants is not null)
        {
            foreach (var cv in CompoundVariants)
            {
                _compoundVariants.Add(ownerObj =>
                {
                    if (ownerObj is not TOwner owner) return null;
                    try
                    {
                        if (!cv.Predicate(owner)) return null;

                        var map = new Dictionary<string, string?>(StringComparer.Ordinal);

                        if (!string.IsNullOrEmpty(cv.Class))
                        {
                            map[GetSlot<TSlots>(s => s.Base)] = cv.Class;
                        }

                        foreach (var pairs in cv)
                        {
                            var slots = pairs.Value;
                            if (slots is null) continue;

                            foreach (var kv in slots)
                            {
                                map[GetSlot(kv.Key)] = (string)kv.Value!;
                            }
                        }

                        return map;
                    }
                    catch
                    {
                        return null;
                    }
                });
            }
        }

        if (_variants.Count == 0)
        {
            _variants = null;
        }

        if (_compoundVariants.Count == 0)
        {
            _compoundVariants = null;
        }
    }

    private void Precompute()
    {
        // typed precomputation
        BaseSlots = PrecomputeBaseAndTopLevelSlots();
        BaseVariants = PrecomputeVariantDefinitions();

        // build adapters for the untyped surface (ITwDescriptor)
        BuildUntypedAdapters();
    }

    private Dictionary<string, string?> PrecomputeBaseAndTopLevelSlots()
    {
        var map = new Dictionary<string, string?>(StringComparer.Ordinal);

        if (Base is not null)
        {
            map[GetSlot<TSlots>(s => s.Base)] = (string)Base;
        }

        if (Slots is not null)
        {
            foreach (var (key, value) in Slots)
            {
                if (value is not null)
                {
                    map[GetSlot(key)] = (string)value;
                }
            }
        }

        return map;
    }

    private Dictionary<string, CompiledVariant<TOwner, TSlots>> PrecomputeVariantDefinitions()
    {
        var variants = new Dictionary<string, CompiledVariant<TOwner, TSlots>>(StringComparer.Ordinal);

        if (Variants is not null)
        {
            foreach (var (key, value) in Variants)
            {
                var id = key.ToString() ?? Guid.NewGuid().ToString();
                var accessor = key.Compile();
                variants[id] = new CompiledVariant<TOwner, TSlots>(key, value, accessor);
            }
        }

        return variants;
    }
}