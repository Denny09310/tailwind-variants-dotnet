using System;
using System.Linq;
using Tw = TailwindMerge.TwMerge;

namespace TailwindVariants.NET.Tests;

#pragma warning disable CS0436

#region Test Models

public partial class ButtonComponent : ISlotted<ButtonSlots>
{
    public string? Class { get; set; }
    public ButtonSlots? Classes { get; set; }
    public bool IsDisabled { get; set; }
    public bool IsLoading { get; set; }
    public string? Size { get; set; }
    public string? Variant { get; set; }
}

public partial class ButtonSlots : ISlots
{
    [Slot("root")]
    public string? Base { get; set; }

    public string? Icon { get; set; }

    public string? Label { get; set; }
}

public partial class GhostButtonSlots : ButtonSlots
{
    public string? Overlay { get; set; }
}

public partial class GhostButtonComponent : ISlotted<GhostButtonSlots>
{
    public string? Class { get; set; }
    public GhostButtonSlots? Classes { get; set; }
    public string? Variant { get; set; }
}

public class TestComponent : ISlotted<TestSlots>
{
    public string? Class { get; set; }
    public TestSlots? Classes { get; set; }
    public string? Color { get; set; }
    public bool IsDisabled { get; set; }
    public string? Size { get; set; }
}

public partial class TestSlots : ISlots
{
    public string? Base { get; set; }

    public string? Container { get; set; }

    [Slot("descr")]
    public string? Description { get; set; }

    public string? Title { get; set; }
}

#endregion Test Models

public class TwVariantsTests
{
    private readonly TwVariants _tv = new(new Tw());

    [Fact]
    public void Invoke_AccessingNonInitializedSlot_ReturnsNull()
    {
        // Arrange
        var descriptor = new TvDescriptor<TestComponent, TestSlots>(
            @base: "container"
        // Container and Title slots are not initialized
        );
        var component = new TestComponent();

        // Act
        var result = _tv.Invoke(component, descriptor);

        // Assert
        Assert.Equal("container", result[s => s.Base]);
        Assert.Null(result[s => s.Container]);
        Assert.Null(result[s => s.Title]);
        Assert.Null(result[s => s.Description]);
    }

    [Fact]
    public void Invoke_GetName_ReturnsCorrectSlot()
    {
        // Arrange
        var descriptor = new TvDescriptor<TestComponent, TestSlots>(
            @base: "container"
        );
        var component = new TestComponent();

        // Act
        var result = _tv.Invoke(component, descriptor);

        // Assert
        Assert.Equal("descr", result.GetName(TestSlotsTypes.Description));
    }

    [Fact]
    public void Invoke_ComplexButtonExample_ProducesCorrectOutput()
    {
        // Arrange
        var descriptor = new TvDescriptor<ButtonComponent, ButtonSlots>(
            @base: "inline-flex items-center justify-center rounded-md font-medium transition-colors",
            slots: new SlotCollection<ButtonSlots>
            {
                [s => s.Icon] = "w-4 h-4",
                [s => s.Label] = "ml-2"
            },
            variants: new VariantCollection<ButtonComponent, ButtonSlots>
            {
                {
                    c => c.Variant,
                    new Variant<string, ButtonSlots>
                    {
                        ["default"] = "bg-primary text-primary-foreground hover:bg-primary/90",
                        ["destructive"] = "bg-destructive text-destructive-foreground hover:bg-destructive/90",
                        ["outline"] = "border border-input bg-background hover:bg-accent"
                    }
                },
                {
                    c => c.Size,
                    new Variant<string, ButtonSlots>
                    {
                        ["sm"] = "h-9 px-3 text-sm",
                        ["md"] = "h-10 px-4 py-2",
                        ["lg"] = "h-11 px-8 text-lg"
                    }
                }
            },
            compoundVariants:
            [
                new(c => c.IsLoading || c.IsDisabled)
                {
                    Class = "opacity-50 pointer-events-none"
                }
            ]
        );

        var button = new ButtonComponent
        {
            Variant = "destructive",
            Size = "lg",
            IsDisabled = true
        };

        // Act
        var result = _tv.Invoke(button, descriptor);

        // Assert
        var baseClasses = result[s => s.Base];
        Assert.Contains("inline-flex", baseClasses);
        Assert.Contains("bg-destructive", baseClasses);
        Assert.Contains("h-11", baseClasses);
        Assert.Contains("px-8", baseClasses);
        Assert.Contains("opacity-50", baseClasses);
        Assert.Contains("pointer-events-none", baseClasses);
    }

    [Fact]
    public void Invoke_MultipleInvocations_DoesNotMutateDescriptor()
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

        // Act
        var result1 = _tv.Invoke(new TestComponent { Size = "sm" }, descriptor);
        var result2 = _tv.Invoke(new TestComponent { Size = "lg" }, descriptor);

        // Assert
        Assert.Contains("text-sm", result1[s => s.Base]);
        Assert.DoesNotContain("text-lg", result1[s => s.Base]);

        Assert.Contains("text-lg", result2[s => s.Base]);
        Assert.DoesNotContain("text-sm", result2[s => s.Base]);
    }

    [Fact]
    public void Invoke_WithBaseClassOnly_ReturnsBaseSlot()
    {
        // Arrange
        var descriptor = new TvDescriptor<TestComponent, TestSlots>(
            @base: "p-4 bg-white"
        );
        var component = new TestComponent();

        // Act
        var result = _tv.Invoke(component, descriptor);

        // Assert
        Assert.Equal("p-4 bg-white", result[s => s.Base]);
    }

    [Fact]
    public void Invoke_WithClassesContainingNullSlot_SkipsNullSlots()
    {
        // Arrange
        var descriptor = new TvDescriptor<TestComponent, TestSlots>(
            @base: "component",
            slots: new()
            {
                [s => s.Title] = "text-lg"
            }
        );
        var component = new TestComponent
        {
            Classes = new TestSlots
            {
                Title = "font-bold",
                Description = null  // Null slot override
            }
        };

        // Act
        var result = _tv.Invoke(component, descriptor);

        // Assert
        Assert.Contains("text-lg", result[s => s.Title]);
        Assert.Contains("font-bold", result[s => s.Title]);
        Assert.Null(result[s => s.Description]);
    }

    [Fact]
    public void Invoke_WithClassesOverride_AppendsToSpecificSlots()
    {
        // Arrange
        var descriptor = new TvDescriptor<TestComponent, TestSlots>(
            @base: "component",
            slots: new()
            {
                [s => s.Title] = "text-lg",
                [s => s.Description] = "text-sm"
            }
        );
        var component = new TestComponent
        {
            Classes = new TestSlots
            {
                Title = "font-extrabold",
                Description = "italic"
            }
        };

        // Act
        var result = _tv.Invoke(component, descriptor);

        // Assert
        Assert.Contains("text-lg", result[s => s.Title]);
        Assert.Contains("font-extrabold", result[s => s.Title]);
        Assert.Contains("text-sm", result[s => s.Description]);
        Assert.Contains("italic", result[s => s.Description]);
    }

    [Fact]
    public void Invoke_WithClassOverride_AppendsToBaseSlot()
    {
        // Arrange
        var descriptor = new TvDescriptor<TestComponent, TestSlots>(
            @base: "btn bg-blue-500"
        );
        var component = new TestComponent { Class = "hover:bg-blue-600" };

        // Act
        var result = _tv.Invoke(component, descriptor);

        // Assert
        Assert.Contains("btn", result[s => s.Base]);
        Assert.Contains("bg-blue-500", result[s => s.Base]);
        Assert.Contains("hover:bg-blue-600", result[s => s.Base]);
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
    public void Invoke_WithEmptyDescriptor_ReturnsEmptyBaseSlot()
    {
        // Arrange
        var descriptor = new TvDescriptor<TestComponent, TestSlots>();
        var component = new TestComponent();

        // Act
        var result = _tv.Invoke(component, descriptor);

        // Assert
        Assert.Null(result[s => s.Base]);
    }

    [Fact]
    public void Invoke_WithEmptyStringBase_ReturnsEmptyBaseSlot()
    {
        // Arrange
        var descriptor = new TvDescriptor<TestComponent, TestSlots>(
            @base: ""
        );
        var component = new TestComponent();

        // Act
        var result = _tv.Invoke(component, descriptor);

        // Assert
        Assert.Null(result[s => s.Base]);
    }

    [Fact]
    public void Invoke_WithEmptyStringClassOverride_AppendsEmpty()
    {
        // Arrange
        var descriptor = new TvDescriptor<TestComponent, TestSlots>(
            @base: "btn"
        );
        var component = new TestComponent { Class = "" };

        // Act
        var result = _tv.Invoke(component, descriptor);

        // Assert
        Assert.Equal("btn", result[s => s.Base]);
    }

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
        var result = _tv.Invoke(component, descriptor);

        // Assert
        var baseClasses = result[s => s.Base];
        Assert.Contains("btn", baseClasses);
        Assert.Contains("text-lg", baseClasses);
        Assert.Contains("py-3", baseClasses);
        Assert.Contains("bg-blue-500", baseClasses);
        Assert.Contains("text-white", baseClasses);
    }

    [Fact]
    public void Invoke_WithNullBaseValue_ReturnsEmptyBaseSlot()
    {
        // Arrange
        var descriptor = new TvDescriptor<TestComponent, TestSlots>(
            @base: null
        );
        var component = new TestComponent();

        // Act
        var result = _tv.Invoke(component, descriptor);

        // Assert
        Assert.Null(result[s => s.Base]);
    }

    [Fact]
    public void Invoke_WithNullClassesObject_HandlesGracefully()
    {
        // Arrange
        var descriptor = new TvDescriptor<TestComponent, TestSlots>(
            @base: "component",
            slots: new()
            {
                [s => s.Title] = "text-lg"
            }
        );
        var component = new TestComponent { Classes = null };

        // Act
        var result = _tv.Invoke(component, descriptor);

        // Assert
        Assert.Equal("component", result[s => s.Base]);
        Assert.Equal("text-lg", result[s => s.Title]);
    }

    [Fact]
    public void Invoke_WithNullClassOverride_IgnoresOverride()
    {
        // Arrange
        var descriptor = new TvDescriptor<TestComponent, TestSlots>(
            @base: "btn bg-blue-500"
        );
        var component = new TestComponent { Class = null };

        // Act
        var result = _tv.Invoke(component, descriptor);

        // Assert
        Assert.Equal("btn bg-blue-500", result[s => s.Base]);
    }

    [Fact]
    public void Invoke_WithNullSlotValue_ReturnsEmptySlot()
    {
        // Arrange
        var descriptor = new TvDescriptor<TestComponent, TestSlots>(
            @base: "container",
            slots: new()
            {
                [s => s.Title] = null,
                [s => s.Description] = "text-sm"
            }
        );
        var component = new TestComponent();

        // Act
        var result = _tv.Invoke(component, descriptor);

        // Assert
        Assert.Equal("container", result[s => s.Base]);
        Assert.Null(result[s => s.Title]);
        Assert.Equal("text-sm", result[s => s.Description]);
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
        var result = _tv.Invoke(component, descriptor);

        // Assert
        Assert.Equal("btn", result[s => s.Base]);
        Assert.DoesNotContain("text-sm", result[s => s.Base]);
        Assert.DoesNotContain("text-lg", result[s => s.Base]);
    }

    [Fact]
    public void Invoke_WithSlots_ReturnsAllSlots()
    {
        // Arrange
        var descriptor = new TvDescriptor<TestComponent, TestSlots>(
            @base: "container",
            slots: new()
            {
                [s => s.Title] = "text-xl font-bold",
                [s => s.Description] = "text-sm text-gray-600"
            }
        );
        var component = new TestComponent();

        // Act
        var result = _tv.Invoke(component, descriptor);

        // Assert
        Assert.Equal("container", result[s => s.Base]);
        Assert.Equal("text-xl font-bold", result[s => s.Title]);
        Assert.Equal("text-sm text-gray-600", result[s => s.Description]);
    }

    [Fact]
    public void Invoke_WithTailwindMerge_ResolvesConflictingClasses()
    {
        // Arrange
        var descriptor = new TvDescriptor<TestComponent, TestSlots>(
            @base: "p-4 bg-red-500"
        );
        var component = new TestComponent { Class = "p-8 bg-blue-500" };

        // Act
        var result = _tv.Invoke(component, descriptor);

        // Assert
        // TailwindMerge should keep the last conflicting class
        Assert.Contains("p-8", result[s => s.Base]);
        Assert.Contains("bg-blue-500", result[s => s.Base]);
        Assert.DoesNotContain("p-4", result[s => s.Base]);
        Assert.DoesNotContain("bg-red-500", result[s => s.Base]);
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
                    ["sm"] = null,  // Null variant value
                    ["lg"] = "text-lg"
                }
            }
            }
        );
        var component = new TestComponent { Size = "sm" };

        // Act
        var result = _tv.Invoke(component, descriptor);

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
        var result = _tv.Invoke(component, descriptor);

        // Assert
        var baseClasses = result[s => s.Base];
        Assert.Contains("text-lg", baseClasses);
        Assert.Contains("py-3", baseClasses);
        Assert.Contains("px-6", baseClasses);
    }

    [Fact]
    public void EnumerateOverrides_WithDerivedClass_IncludesBaseProperties()
    {
        // Arrange
        var slots = new GhostButtonSlots
        {
            Base = "root-class",
            Icon = "icon-class",
            Label = "label-class",
            Overlay = "overlay-class"
        };

        // Act
        var overrides = slots.EnumerateOverrides().ToList();

        // Assert
        Assert.Equal(4, overrides.Count);

        // Check base class properties are included
        Assert.Contains(overrides, o => o.Slot == "root" && o.Value == "root-class");
        Assert.Contains(overrides, o => o.Slot == "Icon" && o.Value == "icon-class");
        Assert.Contains(overrides, o => o.Slot == "Label" && o.Value == "label-class");

        // Check derived class property is included
        Assert.Contains(overrides, o => o.Slot == "Overlay" && o.Value == "overlay-class");
    }

    [Fact]
    public void EnumerateOverrides_WithBaseClass_OnlyIncludesBaseProperties()
    {
        // Arrange
        var slots = new ButtonSlots
        {
            Base = "root-class",
            Icon = "icon-class",
            Label = "label-class"
        };

        // Act
        var overrides = slots.EnumerateOverrides().ToList();

        // Assert
        Assert.Equal(3, overrides.Count);
        Assert.Contains(overrides, o => o.Slot == "root" && o.Value == "root-class");
        Assert.Contains(overrides, o => o.Slot == "Icon" && o.Value == "icon-class");
        Assert.Contains(overrides, o => o.Slot == "Label" && o.Value == "label-class");
    }

    [Fact]
    public void EnumerateOverrides_WithDerivedClassPartialProperties_OnlyIncludesNonNull()
    {
        // Arrange
        var slots = new GhostButtonSlots
        {
            Base = "root-class",
            // Icon and Label are null
            Overlay = "overlay-class"
        };

        // Act
        var overrides = slots.EnumerateOverrides().ToList();

        // Assert
        Assert.Equal(2, overrides.Count);
        Assert.Contains(overrides, o => o.Slot == "root" && o.Value == "root-class");
        Assert.Contains(overrides, o => o.Slot == "Overlay" && o.Value == "overlay-class");
    }

    [Fact]
    public void GetName_WithBaseClassProperty_ReturnsCorrectSlotName()
    {
        // Act & Assert
        Assert.Equal("root", ButtonSlots.GetName(nameof(ButtonSlots.Base)));
        Assert.Equal("Icon", ButtonSlots.GetName(nameof(ButtonSlots.Icon)));
        Assert.Equal("Label", ButtonSlots.GetName(nameof(ButtonSlots.Label)));
    }

    [Fact]
    public void GetName_WithDerivedClassProperty_ReturnsCorrectSlotName()
    {
        // Act & Assert
        Assert.Equal("Overlay", GhostButtonSlots.GetName(nameof(GhostButtonSlots.Overlay)));
    }

    [Fact]
    public void GetName_WithBaseClassPropertyOnDerivedClass_ReturnsCorrectSlotName()
    {
        // Act & Assert - derived class should resolve base class properties
        Assert.Equal("root", GhostButtonSlots.GetName(nameof(GhostButtonSlots.Base)));
        Assert.Equal("Icon", GhostButtonSlots.GetName(nameof(GhostButtonSlots.Icon)));
        Assert.Equal("Label", GhostButtonSlots.GetName(nameof(GhostButtonSlots.Label)));
    }

    [Fact]
    public void GetName_WithUnknownProperty_ReturnsPropertyName()
    {
        // Act & Assert
        Assert.Equal("Unknown", ButtonSlots.GetName("Unknown"));
        Assert.Equal("Unknown", GhostButtonSlots.GetName("Unknown"));
    }

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
        Assert.Single(enumValues);
    }

    [Fact]
    public void SlotsNames_ForBaseClass_ReturnsCorrectNames()
    {
        // Act & Assert
        Assert.Equal("root", ButtonSlotsNames.Base);
        Assert.Equal("Icon", ButtonSlotsNames.Icon);
        Assert.Equal("Label", ButtonSlotsNames.Label);
    }

    [Fact]
    public void SlotsNames_ForDerivedClass_ReturnsCorrectNames()
    {
        // Act & Assert
        Assert.Equal("Overlay", GhostButtonSlotsNames.Overlay);
    }

    [Fact]
    public void SlotsNames_NameOf_ReturnsCorrectName()
    {
        // Act & Assert
        Assert.Equal("Base", ButtonSlotsNames.NameOf(ButtonSlotsTypes.Base));
        Assert.Equal("Icon", ButtonSlotsNames.NameOf(ButtonSlotsTypes.Icon));
        Assert.Equal("Overlay", GhostButtonSlotsNames.NameOf(GhostButtonSlotsTypes.Overlay));
    }

    [Fact]
    public void SlotsNames_AllNames_ReturnsAllPropertyNames()
    {
        // Act
        var baseNames = ButtonSlotsNames.AllNames;
        var derivedNames = GhostButtonSlotsNames.AllNames;

        // Assert
        Assert.Equal(3, baseNames.Count);
        Assert.Contains("Base", baseNames);
        Assert.Contains("Icon", baseNames);
        Assert.Contains("Label", baseNames);

        Assert.Single(derivedNames);
        Assert.Contains("Overlay", derivedNames);
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
}

#pragma warning restore CS0436
