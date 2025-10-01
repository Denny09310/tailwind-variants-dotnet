using System.Linq.Expressions;

namespace TailwindVariants.NET;

/// <summary>
/// Helper methods for working with slot accessor expressions.
/// </summary>
internal static class TvHelpers
{
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