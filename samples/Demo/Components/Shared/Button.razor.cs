using System.Linq.Expressions;
using TailwindVariants;

namespace Demo.Components.Shared;

public partial class Button : ISlottableComponent<Button.Slots>
{
    private static readonly VariantConfig<Button, Slots> _variants = new VariantBuilder<Button, Slots>()
        .Base("inline-flex items-center justify-center font-semibold border transition-shadow focus:outline-none")
        .Slot(b => b.Icon, "inline-flex items-center")
        .Variant(b => b.Variant, new Dictionary<Variants, string>
        {
            [Variants.Default] = "bg-white text-gray-800 border-gray-300 hover:bg-gray-50",
            [Variants.Primary] = "bg-blue-500 text-white border-transparent shadow",
            [Variants.Secondary] = "bg-gray-100 text-gray-800 border-gray-300",
            [Variants.Ghost] = "bg-transparent text-gray-800 border-transparent",
        })
        .Variant(b => b.Size, new Dictionary<Sizes, string>
        {
            [Sizes.Small] = "text-sm px-3 py-1",
            [Sizes.Medium] = "text-base px-4 py-2",
            [Sizes.Large] = "text-lg px-5 py-3",
        })
        .Variant(b => b.Radius, new Dictionary<Radii, string>
        {
            [Radii.None] = "rounded-none",
            [Radii.Small] = "rounded-sm",
            [Radii.Medium] = "rounded-md",
            [Radii.Large] = "rounded-lg",
            [Radii.Full] = "rounded-full",
        })
        .Variant(b => b.Disabled, new Dictionary<bool, string>
        {
            [true] = "opacity-50 cursor-not-allowed pointer-events-none"
        })
        .Variant(b => b.IconOnly, new Dictionary<bool, VariantTargets>
        {
            [true] = new VariantTargets
            (
                root: "p-2"
            ),
            [false] = new VariantTargets
            (
                slots: new()
                {
                    [nameof(Slots.Icon)] = "mr-2"
                }
            ),
        })
        // compounds for hover and uppercase when primary+medium etc.
        .Compound(b => b.Variant == Variants.Primary && !b.Disabled, "hover:bg-blue-600")
        .Compound(b => b.Variant == Variants.Secondary && !b.Disabled, "hover:bg-gray-200")
        .Compound(b => b.Variant == Variants.Primary && b.Size == Sizes.Medium, "uppercase tracking-wide")
        .Compound(b => b.IconOnly && b.Size == Sizes.Small, "w-8 h-8")
        .Compound(b => b.IconOnly && b.Size == Sizes.Medium, "w-10 h-10")
        .Compound(b => b.IconOnly && b.Size == Sizes.Large, "w-12 h-12")
        .Build();

    public enum Radii
    { None, Small, Medium, Large, Full }

    public enum Sizes
    { Small, Medium, Large }

    public enum Variants
    { Default, Primary, Secondary, Ghost }

    public class Slots
    {
        public string? Icon { get; set; }
    }

    private string? GetClasses() => _variants.GetClasses(this, Tw, Class);
    private string? GetSlot(Expression<Func<Slots, string?>> accessor) => _variants.GetSlot(this, Tw, accessor);
}