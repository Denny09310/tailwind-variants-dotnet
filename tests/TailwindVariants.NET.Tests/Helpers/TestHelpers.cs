using System.Linq.Expressions;

namespace TailwindVariants.NET;

internal static class TestHelpers
{
	public static void ContainsAll<TSlots>(this SlotsMap<TSlots> map, Expression<SlotAccessor<TSlots>> accessor, string? classes = default, string[]? expectedTokens = null)
		where TSlots : ISlots, new()
	{
		var tokens = GetTokenSet(map, accessor, classes);
		foreach (var t in expectedTokens ?? [])
		{
			Assert.Contains(t, tokens);
		}
	}

	public static void DoesNotContainAny<TSlots>(this SlotsMap<TSlots> map, Expression<SlotAccessor<TSlots>> accessor, string[] expectedTokens)
		where TSlots : ISlots, new()
	{
		var tokens = GetTokenSet(map, accessor);
		foreach (var t in expectedTokens)
		{
			Assert.DoesNotContain(t, tokens);
		}
	}

	/// <summary>Exact equality (string) check for a slot value.</summary>
	public static void ShouldEqual<TSlots>(this SlotsMap<TSlots> map, Expression<SlotAccessor<TSlots>> accessor, string expected)
		where TSlots : ISlots, new()
	{
		var value = map[accessor](default);
		Assert.Equal(expected, value);
	}

	private static HashSet<string> GetTokenSet<TSlots>(SlotsMap<TSlots> map, Expression<SlotAccessor<TSlots>> accessor, string? classes = default)
		where TSlots : ISlots, new()
	{
		var result = map[accessor](classes);
		Assert.False(string.IsNullOrWhiteSpace(result));
		return Tokens(result).ToHashSet(StringComparer.Ordinal);
	}

	private static IEnumerable<string> Tokens(string? classes)
		=> (classes ?? string.Empty)
			.Split([' '], StringSplitOptions.RemoveEmptyEntries)
			.Select(s => s.Trim());
}
