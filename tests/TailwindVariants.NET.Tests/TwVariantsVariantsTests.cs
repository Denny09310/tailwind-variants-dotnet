using Tw = TailwindMerge.TwMerge;

namespace TailwindVariants.NET.Tests;

public class TwVariantsVariantsTests : TestContext
{
	public TwVariantsVariantsTests() => Services.AddTailwindVariants();

	[Fact]
	public void Invoke_WithMultipleVariants_CombinesClasses()
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
						["sm"] = "text-sm py-1",
						["lg"] = "text-lg py-3"
					}
				},
				{
					c => c.Color,
					new Variant<string, TestSlots>
					{
						["primary"] = "bg-blue-500 text-white",
						["secondary"] = "bg-gray-500 text-white"
					}
				}
			}
		);
		var component = new TestComponent { Size = "lg", Color = "primary" };

		// Act
		var tv = Services.GetRequiredService<TwVariants>();
		var result = tv.Invoke(component, descriptor);

		// Assert
		var baseClasses = result[s => s.Base];
		Assert.Contains("btn", baseClasses);
		Assert.Contains("text-lg", baseClasses);
		Assert.Contains("py-3", baseClasses);
		Assert.Contains("bg-blue-500", baseClasses);
		Assert.Contains("text-white", baseClasses);
	}

	[Fact]
	public void Invoke_WithNullVariantValue_SkipsVariant()
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
			}
		);
		var component = new TestComponent { Size = null };

		// Act
		var tv = Services.GetRequiredService<TwVariants>();
		var result = tv.Invoke(component, descriptor);

		// Assert
		Assert.Equal("btn", result[s => s.Base]);
		Assert.DoesNotContain("text-sm", result[s => s.Base]);
		Assert.DoesNotContain("text-lg", result[s => s.Base]);
	}

	[Fact]
	public void Invoke_WithVariantContainingNullSlotValue_HandlesGracefully()
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
						["sm"] = null,
						["lg"] = "text-lg"
					}
				}
			}
		);
		var component = new TestComponent { Size = "sm" };

		// Act
		var tv = Services.GetRequiredService<TwVariants>();
		var result = tv.Invoke(component, descriptor);

		// Assert
		Assert.Equal("btn", result[s => s.Base]);
	}

	[Fact]
	public void Invoke_WithVariants_AppliesCorrectVariant()
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
						["sm"] = "text-sm py-1 px-2",
						["md"] = "text-base py-2 px-4",
						["lg"] = "text-lg py-3 px-6"
					}
				}
			}
		);
		var component = new TestComponent { Size = "lg" };

		// Act
		var tv = Services.GetRequiredService<TwVariants>();
		var result = tv.Invoke(component, descriptor);

		// Assert
		var baseClasses = result[s => s.Base];
		Assert.Contains("text-lg", baseClasses);
		Assert.Contains("py-3", baseClasses);
		Assert.Contains("px-6", baseClasses);
	}
}
