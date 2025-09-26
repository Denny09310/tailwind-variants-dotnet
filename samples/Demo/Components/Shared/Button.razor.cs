using TailwindVariants;

namespace Demo.Components.Shared;

public partial class Button : IHasSlots<Button.Slots>
{
    private readonly TvOverrides<Button, Slots> _variants;

    public Button() => _variants = Tv.Create<Button, Slots>(this, new()
    {
        Base = "inline-flex items-center justify-center font-semibold border transition-shadow focus:outline-none",
        Slots = new()
        {
            Icon = "",
        },
        Variants =
        {
            new(b => b.Variant)
            {
                [Variants.Default] = "inline-flex items-center justify-center font-semibold border transition-shadow focus:outline-none",
                [Variants.Primary] = "bg-blue-500 text-white border-transparent shadow",
                [Variants.Secondary] = "bg-gray-100 text-gray-800 border-gray-300",
                [Variants.Ghost] = "bg-transparent text-gray-800 border-transparent",
            },
            new(b => b.Size)
            {
                [Sizes.Small] = "text-sm px-3 py-1",
                [Sizes.Medium] = "text-base px-4 py-2",
                [Sizes.Large] = "text-lg px-5 py-3",
            },
            new(b => b.Radius)
            {
                [Radii.None] = "rounded-none",
                [Radii.Small] = "rounded-sm",
                [Radii.Medium] = "rounded-md",
                [Radii.Large] = "rounded-lg",
                [Radii.Full] = "rounded-full",
            },
            new(b => b.Disabled)
            {
                [true] = "opacity-50 cursor-not-allowed pointer-events-none"
            },
            new(b => b.IconOnly)
            {
                [true] = "p-2",
                [false] = new SlotMap<Slots>()
                { 
                    [s => s.Icon] = "mr-2"
                }
            }
        },
        CompoundVariants =
        {
            new(b => b.Variant == Variants.Primary && !b.Disabled, new()
            {
                [s => s.Base] = "hover:bg-blue-600"
            }),
            new(b => b.Variant == Variants.Secondary && !b.Disabled, new()
            {
                [s => s.Base] = "hover:bg-gray-200",
            })
        }
    })
    .Build();

    public enum Radii
    { None, Small, Medium, Large, Full }

    public enum Sizes
    { Small, Medium, Large }

    public enum Variants
    { Default, Primary, Secondary, Ghost }

    public class Slots : ISlots
    {
        public string? Base { get; set; }
        public string? Icon { get; set; }
    }

    private string? GetClasses() => _variants(Tw);
}