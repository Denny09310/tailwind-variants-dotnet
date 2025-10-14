using System;

using Tw = TailwindMerge.TwMerge;

namespace TailwindVariants.NET.Tests;

public class SlotsInheritanceTests
{
	private readonly TwVariants _tv = new(new Tw());

	[Fact]
	public void Enum_ForBaseClass_ContainsBaseProperties()
	{
		// Assert
		var enumValues = Enum.GetValues<ButtonSlotsTypes>();

		Assert.Contains(ButtonSlotsTypes.Base, enumValues);
		Assert.Contains(ButtonSlotsTypes.Icon, enumValues);
		Assert.Contains(ButtonSlotsTypes.Label, enumValues);
		Assert.Equal(3, enumValues.Length);
	}

	[Fact]
	public void Enum_ForDerivedClass_ContainsOnlyDerivedProperties()
	{
		// Assert - derived enum should only have Overlay, not base properties
		var enumValues = Enum.GetValues<GhostButtonSlotsTypes>();

		Assert.Contains(GhostButtonSlotsTypes.Overlay, enumValues);
		Assert.Equal(4, enumValues.Length);
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
		var result = _tv.Invoke(component, descriptor);

		// Assert
		Assert.Equal("btn", result[s => s.Base]);
		Assert.Equal("w-4 h-4", result[s => s.Icon]);
		Assert.Equal("ml-2", result[s => s.Label]);
		Assert.Equal("absolute inset-0 bg-black/10", result[s => s.Overlay]);
	}

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
		var result = _tv.Invoke(component, descriptor);

		// Assert
		Assert.Contains("bg-transparent", result[s => s.Base]);
		Assert.Contains("absolute", result[s => s.Overlay]);
		Assert.Contains("bg-black/5", result[s => s.Overlay]);
		Assert.Contains("hover:bg-black/10", result[s => s.Overlay]);
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
		var result = _tv.Invoke(component, descriptor);

		// Assert
		Assert.Contains("w-4", result[s => s.Icon]);
		Assert.Contains("text-blue-500", result[s => s.Icon]);
		Assert.Equal("font-bold", result[s => s.Label]);
		Assert.Contains("absolute", result[s => s.Overlay]);
		Assert.Contains("bg-black/20", result[s => s.Overlay]);
	}
}
