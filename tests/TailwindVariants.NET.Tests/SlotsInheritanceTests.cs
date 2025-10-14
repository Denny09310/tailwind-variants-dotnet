namespace TailwindVariants.NET.Tests;

public class SlotsInheritanceTests : TestContext
{
	public SlotsInheritanceTests() => Services.AddTailwindVariants();

	public static TheoryData<Type, string[], int> EnumExpectations { get; } = new()
	{
		{ typeof(ButtonSlotTypes), new[] { "Base", "Icon", "Label" }, 3 },
		{ typeof(GhostButtonSlotTypes), new[] { "Overlay" }, 4 }
	};

	private TwVariants Tv => Services.GetRequiredService<TwVariants>();

	[Theory]
	[MemberData(nameof(EnumExpectations))]
	public void Enums_ContainExpectedValues(Type enumType, string[] mustContainNames, int expectedCount)
	{
		// Act
		var values = Enum.GetNames(enumType);

		// Assert expected names exist
		foreach (var n in mustContainNames)
		{
			Assert.Contains(n, values);
		}

		Assert.Equal(expectedCount, values.Length);
	}

	[Fact]
	public void Invoke_WithDerivedSlots_AppliesAllSlots()
	{
		// Arrange
		var descriptor = new TvDescriptor<GhostButtonComponent, GhostButtonSlots>(
			@base: "btn",
			slots: new()
			{
				[s => s.Icon] = "w-4 h-4",
				[s => s.Label] = "ml-2",
				[s => s.Overlay] = "absolute inset-0 bg-black/10"
			}
		);
		var component = new GhostButtonComponent();

		// Act
		var result = Tv.Invoke(component, descriptor);

		// Assert - exact expectations for each slot
		result.ShouldEqual(s => s.Base, "btn");
		result.ContainsAll(s => s.Icon, "w-4", "h-4");
		result.ContainsAll(s => s.Label, "ml-2");
		result.ContainsAll(s => s.Overlay, "absolute", "inset-0", "bg-black/10");
	}

	// --- Invocation tests -------------------------------------------------------
	[Fact]
	public void Invoke_WithDerivedSlotsAndVariants_CombinesCorrectly()
	{
		// Arrange
		var descriptor = new TvDescriptor<GhostButtonComponent, GhostButtonSlots>(
			@base: "btn",
			slots: new()
			{
				[s => s.Overlay] = "absolute inset-0"
			},
			variants: new VariantCollection<GhostButtonComponent, GhostButtonSlots>
			{
				{
					c => c.Variant,
					new Variant<string, GhostButtonSlots>
					{
						["ghost"] = new()
						{
							[s => s.Base] = "bg-transparent",
							[s => s.Overlay] = "bg-black/5 hover:bg-black/10"
						}
					}
				}
			}
		);
		var component = new GhostButtonComponent { Variant = "ghost" };

		// Act
		var result = Tv.Invoke(component, descriptor);

		// Assert that base and overlay got merged pieces from both base/slot and the variant
		result.ContainsAll(s => s.Base, "bg-transparent");
		result.ContainsAll(s => s.Overlay, "absolute", "inset-0", "bg-black/5", "hover:bg-black/10");
	}

	[Fact]
	public void Invoke_WithDerivedSlotsOverride_MergesAllSlots()
	{
		// Arrange
		var descriptor = new TvDescriptor<GhostButtonComponent, GhostButtonSlots>(
			@base: "btn",
			slots: new()
			{
				[s => s.Icon] = "w-4 h-4",
				[s => s.Overlay] = "absolute inset-0"
			}
		);
		var component = new GhostButtonComponent
		{
			Classes = new GhostButtonSlots
			{
				Icon = "text-blue-500",
				Label = "font-bold",
				Overlay = "bg-black/20"
			}
		};

		// Act
		var result = Tv.Invoke(component, descriptor);

		// Assert
		result.ContainsAll(s => s.Icon, "w-4", "h-4", "text-blue-500");
		result.ContainsAll(s => s.Label, "font-bold");
		result.ContainsAll(s => s.Overlay, "absolute", "inset-0", "bg-black/20");
	}

	[Fact]
	public void Invoke_WithExtends_WithCompoundVariants_AppliesCompoundFromAncestor()
	{
		// Arrange ancestor descriptor with compound variant
		var button = new TvDescriptor<ButtonComponent, ButtonSlots>(
			@base: "font-semibold text-white rounded-full active:opacity-80",
			variants: new()
			{
				[b => b.Variant] = new Variant<string, ButtonSlots>
				{
					["primary"] = "bg-blue-500 hover:bg-blue-700",
					["secondary"] = "bg-purple-500 hover:bg-purple-700",
					["success"] = "bg-green-500 hover:bg-green-700",
				},
				[b => b.Size] = new Variant<string, ButtonSlots>
				{
					["small"] = "py-0 px-2 text-xs",
					["medium"] = "py-1 px-3 text-sm",
					["large"] = "py-1.5 px-3 text-md",
				}
			},
			compoundVariants:
			[
				new(b => b.Variant == "primary" && b.Size == "medium")
				{
					Class = "rounded-sm",
				}
			]
		);

		var descriptor = new TvDescriptor<GhostButtonComponent, GhostButtonSlots>(
			extends: button
		);

		var component = new GhostButtonComponent
		{
			Variant = "primary",
			Size = "medium",
		};

		// Act
		var result = Tv.Invoke(component, descriptor);

		// Assert compound variant effect applied
		result.ContainsAll(s => s.Base,
			"font-semibold",
			"text-white",
			"active:opacity-80",
			"bg-blue-500",
			"hover:bg-blue-700",
			"py-1",
			"px-3",
			"text-sm",
			"rounded-sm");
	}

	[Fact]
	public void Invoke_WithExtends_WithoutVariants_MergesBaseAndExtendsWithoutMutatingDescriptor()
	{
		// Arrange base descriptor (ancestor)
		var button = new TvDescriptor<ButtonComponent, ButtonSlots>(
			@base:
			[
				"font-semibold",
				"dark:text-white",
				"py-1",
				"px-3",
				"rounded-full",
				"active:opacity-80",
				"bg-zinc-100",
				"hover:bg-zinc-200",
				"dark:bg-zinc-800",
				"dark:hover:bg-zinc-800",
			]
		);

		var descriptor = new TvDescriptor<GhostButtonComponent, GhostButtonSlots>(
			extends: button,
			@base:
			[
				"text-sm",
				"text-white",
				"rounded-lg",
				"shadow-lg",
				"uppercase",
				"tracking-wider",
				"bg-blue-500",
				"hover:bg-blue-600",
				"shadow-blue-500/50",
				"dark:bg-blue-500",
				"dark:hover:bg-blue-600",
			]
		);

		var component = new GhostButtonComponent();

		// Act
		var result = Tv.Invoke(component, descriptor);

		// Assert presence of the merged tokens
		result.ContainsAll(s => s.Base,
			"font-semibold",
			"dark:text-white",
			"py-1",
			"px-3",
			"active:opacity-80",
			"shadow-lg",
			"shadow-blue-500/50",
			"uppercase",
			"tracking-wider",
			"bg-blue-500",
			"hover:bg-blue-600",
			"dark:bg-blue-500",
			"dark:hover:bg-blue-600"
		);
	}

	[Fact]
	public void Invoke_WithExtends_WithVariants_AppliesVariantsFromAncestorsAndSelf()
	{
		// Arrange ancestor button descriptor with variants
		var button = new TvDescriptor<ButtonComponent, ButtonSlots>(
			@base: "font-semibold text-white rounded-full active:opacity-80",
			variants: new()
			{
				[b => b.Variant] = new Variant<string, ButtonSlots>
				{
					["primary"] = "bg-blue-500 hover:bg-blue-700",
					["secondary"] = "bg-purple-500 hover:bg-purple-700",
					["success"] = "bg-green-500 hover:bg-green-700",
				},
				[b => b.Size] = new Variant<string, ButtonSlots>
				{
					["small"] = "py-0 px-2 text-xs",
					["medium"] = "py-1 px-3 text-sm",
					["large"] = "py-1.5 px-3 text-md",
				}
			}
		);

		var descriptor = new TvDescriptor<GhostButtonComponent, GhostButtonSlots>(
			extends: button,
			variants: new()
			{
				[b => b.IsSquared] = new Variant<bool, GhostButtonSlots>
				{
					[true] = "rounded-sm"
				}
			}
		);

		var component = new GhostButtonComponent
		{
			Variant = "secondary",
			Size = "medium",
			IsSquared = true,
		};

		// Act
		var result = Tv.Invoke(component, descriptor);

		// Assert expected tokens from ancestor variants and child variants
		result.ContainsAll(s => s.Base,
			"font-semibold",
			"active:opacity-80",
			"rounded-sm",
			"bg-purple-500",
			"hover:bg-purple-700",
			"py-1",
			"px-3",
			"text-sm"
		);
	}
}
