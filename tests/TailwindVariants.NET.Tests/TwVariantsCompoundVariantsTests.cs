using Tw = TailwindMerge.TwMerge;

namespace TailwindVariants.NET.Tests;

public class TwVariantsCompoundVariantsTests
{
	private readonly TwVariants _tv = new(new Tw());

	[Fact]
	public void Invoke_WithCompoundVariants_AppliesWhenPredicateMatches()
	{
		// Arrange
		var descriptor = new TvDescriptor<TestComponent, TestSlots>(
			@base: "btn",
			variants: new VariantCollection<TestComponent, TestSlots>
			{
				{
					c => c.Size,
					new Variant<string, TestSlots>
					{
						["sm"] = "text-sm",
						["lg"] = "text-lg"
					}
				}
			},
			compoundVariants:
			[
				new(c => c.Size == "lg" && c.IsDisabled)
				{
					Class = "opacity-50 cursor-not-allowed"
				}
			]
		);
		var component = new TestComponent { Size = "lg", IsDisabled = true };

		// Act
		var result = _tv.Invoke(component, descriptor);

		// Assert
		Assert.Contains("opacity-50", result[s => s.Base]);
		Assert.Contains("cursor-not-allowed", result[s => s.Base]);
	}

	[Fact]
	public void Invoke_WithCompoundVariants_DoesNotApplyWhenPredicateFails()
	{
		// Arrange
		var descriptor = new TvDescriptor<TestComponent, TestSlots>(
			@base: "btn",
			compoundVariants:
			[
				new(c => c.Size == "lg" && c.IsDisabled)
				{
					Class = "opacity-50"
				}
			]
		);
		var component = new TestComponent { Size = "sm", IsDisabled = true };

		// Act
		var result = _tv.Invoke(component, descriptor);

		// Assert
		Assert.DoesNotContain("opacity-50", result[s => s.Base]);
	}

	[Fact]
	public void Invoke_WithCompoundVariantSlots_AppliesClassesToSpecificSlots()
	{
		// Arrange
		var descriptor = new TvDescriptor<TestComponent, TestSlots>(
			@base: "component",
			slots: new()
			{
				[s => s.Title] = "text-lg",
				[s => s.Description] = "text-sm"
			},
			compoundVariants:
			[
				new(c => c.IsDisabled)
				{
					[s => s.Title] = "text-gray-400",
					[s => s.Description] = "text-gray-300"
				}
			]
		);
		var component = new TestComponent { IsDisabled = true };

		// Act
		var result = _tv.Invoke(component, descriptor);

		// Assert
		Assert.Contains("text-gray-400", result[s => s.Title]);
		Assert.Contains("text-gray-300", result[s => s.Description]);
	}

	[Fact]
	public void Invoke_WithCompoundVariantNullClass_OnlyAppliesSlots()
	{
		// Arrange
		var descriptor = new TvDescriptor<TestComponent, TestSlots>(
			@base: "btn",
			compoundVariants:
			[
				new(c => c.IsDisabled)
				{
					Class = null,
					[s => s.Title] = "text-gray-400"
				}
			],
			slots: new()
			{
				[s => s.Title] = "text-lg"
			}
		);
		var component = new TestComponent { IsDisabled = true };

		// Act
		var result = _tv.Invoke(component, descriptor);

		// Assert
		Assert.Equal("btn", result[s => s.Base]);
		Assert.Contains("text-gray-400", result[s => s.Title]);
	}

	[Fact]
	public void Invoke_WithCompoundVariantEmptyClass_HandlesGracefully()
	{
		// Arrange
		var descriptor = new TvDescriptor<TestComponent, TestSlots>(
			@base: "btn",
			compoundVariants:
			[
				new(c => c.IsDisabled)
				{
					Class = ""
				}
			]
		);
		var component = new TestComponent { IsDisabled = true };

		// Act
		var result = _tv.Invoke(component, descriptor);

		// Assert
		Assert.Equal("btn", result[s => s.Base]);
	}
}
