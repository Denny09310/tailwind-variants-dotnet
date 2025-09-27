using System.Linq.Expressions;

namespace TailwindVariants;

/// <summary>
/// Helper methods for working with slot accessor expressions.
/// </summary>
internal static class TvHelpers
{
    /// <summary>
    /// Enumerates all non-null and non-whitespace string properties of a slots object.
    /// </summary>
    /// <typeparam name="TSlots">The type representing the slots, implementing <see cref="ISlots"/>.</typeparam>
    /// <param name="slots">The instance of the slots object to inspect.</param>
    /// <returns>
    /// An enumerable of tuples containing the property name (slot) and its value,
    /// for each string property that is not null or whitespace.
    /// </returns>
    public static IEnumerable<(string Slot, string Value)> EnumerateClassesOverrides<TSlots>(TSlots slots)
        where TSlots : ISlots
    {
        foreach (var prop in typeof(TSlots).GetProperties())
        {
            if (prop.PropertyType != typeof(string)) continue;

            var value = (string?)prop.GetValue(slots);
            if (!string.IsNullOrWhiteSpace(value))
            {
                yield return (prop.Name, value);
            }
        }
    }

    /// <summary>
    /// Extracts the slot name from a slot accessor expression.
    /// </summary>
    /// <typeparam name="TSlots">The type representing the slots.</typeparam>
    /// <param name="accessor">An expression selecting a slot from the slots type (e.g. <c>s => s.Base</c>).</param>
    /// <returns>The name of the slot as a string.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the expression is not a simple member access (e.g. <c>s => s.Base</c>).
    /// </exception>
    public static string GetSlot<TSlots>(Expression<SlotAccessor<TSlots>> accessor)
        where TSlots : ISlots
    {
        if (accessor.Body is MemberExpression memberExpr)
        {
            return memberExpr.Member.Name;
        }
        // handle conversions like (object) s.Base
        if (accessor.Body is UnaryExpression unary && unary.Operand is MemberExpression member)
        {
            return member.Member.Name;
        }
        throw new ArgumentException($"Invalid slot accessor expression: '{accessor}'. Expression must be a simple member access (e.g. 's => s.Base').", nameof(accessor));
    }
}