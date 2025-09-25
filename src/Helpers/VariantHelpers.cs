using BlazorComponentUtilities;
using System.Linq.Expressions;

namespace TailwindVariants;

/// <summary>
/// Internal helpers used by VariantConfig implementations to avoid duplicate GetClasses code.
/// </summary>
internal static class VariantHelpers
{
    /// <summary>
    /// Build a CSS string from base classes, evaluated variant definitions, compound rules and extra classes.
    /// </summary>
    public static string BuildCssString<T>(T instance, string baseClasses, IEnumerable<IVariantDefinition<T>> variantDefs, IEnumerable<CompoundVariant<T>> compound, params IEnumerable<string?> extraClasses)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(baseClasses)) parts.Add(baseClasses);

        foreach (var def in variantDefs)
        {
            var part = def.GetFor(instance);
            if (!string.IsNullOrWhiteSpace(part)) parts.Add(part);
        }

        foreach (var c in compound)
        {
            if (c.Predicate(instance) && !string.IsNullOrWhiteSpace(c.Classes)) parts.Add(c.Classes);
        }

        // Flatten extra classes collections
        var extraParts = extraClasses?
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray() ?? [];

        if (extraParts.Length > 0) parts.Add(string.Join(" ", extraParts));

        return new CssBuilder(string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p))))
            .Build();
    }

    /// <summary>
    /// Extract a simple property name from a member expression (e.g. s => s.Avatar => "Avatar").
    /// </summary>
    public static string GetVariantKey<T, TValue>(Expression<Func<T, TValue>> accessor)
    {
        if (accessor.Body is MemberExpression member)
            return member.Member.Name;
        throw new ArgumentException("Unsupported expression type for variant key", nameof(accessor));
    }
}