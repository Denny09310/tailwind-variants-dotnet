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

	[Fact]
	public void Invoke_WithExtends_WithoutVariants()
	{
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

		// Arrange
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

		var component = new GhostButtonComponent
		{
		};

		// Act
		var result = _tv.Invoke(component, descriptor);

		// Assert
		var baseClasses = result[s => s.Base];

		Assert.Contains("font-semibold", baseClasses);
		Assert.Contains("dark:text-white", baseClasses);
		Assert.Contains("py-1", baseClasses);
		Assert.Contains("px-3", baseClasses);
		Assert.Contains("active:opacity-80", baseClasses);
		Assert.Contains("shadow-lg", baseClasses);
		Assert.Contains("shadow-blue-500/50", baseClasses);
		Assert.Contains("uppercase", baseClasses);
		Assert.Contains("tracking-wider", baseClasses);
		Assert.Contains("bg-blue-500", baseClasses);
		Assert.Contains("hover:bg-blue-600", baseClasses);
		Assert.Contains("dark:bg-blue-500", baseClasses);
		Assert.Contains("dark:hover:bg-blue-600", baseClasses);
	}

	//[Fact]
	//public void Invoke_WithExtends_WithVariants()
	//{
	//	var button = new TvDescriptor<ButtonComponent, ButtonSlots>(
	//		@base: "font-semibold text-white rounded-full active:opacity-80",
	//		variants: new()
	//		{
	//			[b => b.Variant] = new Variant<string, ButtonSlots>
	//			{
	//				["primary"] = "bg-blue-500 hover:bg-blue-700",
	//				["secondary"] = "bg-purple-500 hover:bg-purple-700",
	//				["success"] = "bg-green-500 hover:bg-green-700",
	//			},
	//			[b => b.Size] = new Variant<string, ButtonSlots>
	//			{
	//				["small"] = "py-0 px-2 text-xs",
	//				["medium"] = "py-1 px-3 text-sm",
	//				["large"] = "py-1.5 px-3 text-md",
	//			}
	//		}
	//	);

	//	// Arrange
	//	var descriptor = new TvDescriptor<GhostButtonComponent, GhostButtonSlots>(
	//		extends: button,
	//		variants: new()
	//		{
	//			[b => b.IsSquared] = new Variant<bool, GhostButtonSlots>
	//			{
	//				[true] = "rounded-sm"
	//			}
	//		}
	//	);

	//	var component = new GhostButtonComponent
	//	{
	//		Variant = "success",
	//		Size = "medium",
	//		IsSquared = true,
	//	};

	//	// Act
	//	var result = _tv.Invoke(component, descriptor);

	//	// Assert
	//	var baseClasses = result[s => s.Base];

	//	Assert.Contains("font-semibold", baseClasses);
	//	Assert.Contains("active:opacity-80", baseClasses);
	//	Assert.Contains("rounded-sm", baseClasses);
	//	Assert.Contains("bg-purple-500", baseClasses);
	//	Assert.Contains("hover:bg-purple-700", baseClasses);
	//	Assert.Contains("py-1", baseClasses);
	//	Assert.Contains("px-3", baseClasses);
	//	Assert.Contains("text-sm", baseClasses);
	//}
}
