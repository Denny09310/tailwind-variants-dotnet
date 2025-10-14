using Tw = TailwindMerge.TwMerge;

namespace TailwindVariants.NET.Tests;

public class TwVariantsVariantsTests : TestContext
{
	public TwVariantsVariantsTests() => Services.AddTailwindVariants();
	private TwVariants Tv => Services.GetRequiredService<TwVariants>();

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
		var result = Tv.Invoke(component, descriptor);

		// Assert
		result.ContainsAll(b => b.Base,
			"btn",
			"text-lg",
			"py-3",
			"bg-blue-500",
			"text-white");
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
		var result = Tv.Invoke(component, descriptor);

		// Assert
		result.ShouldEqual(s => s.Base, "btn");

		result.DoesNotContainAny(b => b.Base,
			"text-sm",
			"text-lg");
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
		var result = Tv.Invoke(component, descriptor);

		// Assert
		result.ShouldEqual(s => s.Base, "btn");
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
		var result = Tv.Invoke(component, descriptor);

		// Assert
		result.ContainsAll(b => b.Base,
			"text-lg",
			"py-3",
			"px-6");
	}
}
