using TailwindVariants.NET;
using static TailwindVariants.NET.TvFunction;

namespace Demo.Components.Shared;

public partial class Link : ISlotted<Link.Slots>
{
    private static readonly TvReturnType<Link, Slots> _link = Tv<Link, Slots>(new()
    {
        Base = "inline-flex items-center font-medium transition-colors",
        Variants = new()
        {
            [l => l.Variant] = new Variant<Variants, Slots>()
            {
                [Variants.Solid] = "rounded-md shadow-sm",
                [Variants.Ghost] = "bg-transparent hover:opacity-75",
                [Variants.Underline] = "underline-offset-2 hover:underline"
            },
            [l => l.Color] = new Variant<Colors, Slots>()
            {
                [Colors.Default] = "text-black dark:text-white",
                [Colors.Primary] = "text-blue-600 hover:text-blue-700",
                [Colors.Secondary] = "text-gray-600 hover:text-gray-800",
                [Colors.Danger] = "text-red-600 hover:text-red-700"
            },
            [l => l.Size] = new Variant<Sizes, Slots>()
            {
                [Sizes.Small] = "text-sm px-2 py-1",
                [Sizes.Medium] = "text-base px-3 py-1.5",
                [Sizes.Large] = "text-lg px-4 py-2"
            }
        },
    });

    private SlotsMap<Slots> _slots = new();

    protected override void OnParametersSet()
    {
        _slots = _link(this, Tw);
    }

    public sealed partial class Slots : ISlots
    {
        public string? Base { get; set; }
    }

    public enum Variants
    {
        Solid,
        Ghost,
        Underline
    }

    public enum Colors
    {
        Default,
        Primary,
        Secondary,
        Danger
    }

    public enum Sizes
    {
        Small,
        Medium,
        Large
    }
}