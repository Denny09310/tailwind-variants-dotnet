using Tw = TailwindMerge.TwMerge;

namespace TailwindVariants.NET.Tests;

public class TwVariantsBasicTests
{
    private readonly TwVariants _tv = new(new Tw());

    [Fact]
    public void Invoke_AccessingNonInitializedSlot_ReturnsNull()
    {
        // Arrange
        var descriptor = new TvDescriptor<TestComponent, TestSlots>(
            @base: "container"
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
}