using System.Linq;
using Tw = TailwindMerge.TwMerge;

namespace TailwindVariants.NET.Tests;

public class SlotsInheritanceTests
{
    private readonly TwVariants _tv = new(new Tw());

    #region EnumerateOverrides Tests

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
        Assert.Contains(overrides, o => o.Slot == "root" && o.Value == "root-class");
        Assert.Contains(overrides, o => o.Slot == "Icon" && o.Value == "icon-class");
        Assert.Contains(overrides, o => o.Slot == "Label" && o.Value == "label-class");
        Assert.Contains(overrides, o => o.Slot == "Overlay" && o.Value == "overlay-class");
    }

    [Fact]
    public void EnumerateOverrides_WithDerivedClassPartialProperties_OnlyIncludesNonNull()
    {
        // Arrange
        var slots = new GhostButtonSlots
        {
            Base = "root-class",
            Overlay = "overlay-class"
        };

        // Act
        var overrides = slots.EnumerateOverrides().ToList();

        // Assert
        Assert.Equal(2, overrides.Count);
        Assert.Contains(overrides, o => o.Slot == "root" && o.Value == "root-class");
        Assert.Contains(overrides, o => o.Slot == "Overlay" && o.Value == "overlay-class");
    }

    #endregion EnumerateOverrides Tests

    #region GetName Tests

    [Fact]
    public void GetName_WithBaseClassProperty_ReturnsCorrectSlotName()
    {
        // Act & Assert
        Assert.Equal("root", ButtonSlots.GetName(nameof(ButtonSlots.Base)));
        Assert.Equal("Icon", ButtonSlots.GetName(nameof(ButtonSlots.Icon)));
        Assert.Equal("Label", ButtonSlots.GetName(nameof(ButtonSlots.Label)));
    }

    #endregion
}