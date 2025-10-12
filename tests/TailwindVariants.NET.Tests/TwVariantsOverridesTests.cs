using Tw = TailwindMerge.TwMerge;

namespace TailwindVariants.NET.Tests;

public class TwVariantsOverridesTests
{
    private readonly TwVariants _tv = new(new Tw());

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
                Description = null
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
        Assert.Contains("p-8", result[s => s.Base]);
        Assert.Contains("bg-blue-500", result[s => s.Base]);
        Assert.DoesNotContain("p-4", result[s => s.Base]);
        Assert.DoesNotContain("bg-red-500", result[s => s.Base]);
    }
}