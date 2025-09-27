using TailwindVariants;
using static TailwindVariants.TvFunction;

namespace Demo.Components.Shared;

public partial class Button
{
    private static readonly TvReturnType<Button, Slots> _button = Tv<Button, Slots>(new()
    {
        Base = "font-medium rounded-full cursor-pointer active:opacity-80",
        Variants = new()
        {
            [b => b.Color] = new Variant<Colors, Slots>()
            {
                [Colors.Primary] = "bg-blue-500 text-white",
                [Colors.Secondary] = "bg-purple-500 text-white",
            },
            [b => b.Size] = new Variant<Sizes, Slots>()
            {
                [Sizes.Small] = "text-sm",
                [Sizes.Medium] = "text-base",
                [Sizes.Large] = "px-4 py-3 text-lg",
            },
            [b => b.Disabled] = new Variant<bool, Slots>()
            {
                [true] = "opacity-50 bg-gray-500 pointer-events-none",
            }
        },
        CompoundVariants =
        [
            new(b => b.Size is Sizes.Small or Sizes.Medium)
            {
                Class = "px-3 py-1"
            },
            new(b => b.Color is Colors.Primary && !b.Disabled)
            {
                Class = "hover:bg-blue-600"
            },
            new(b => b.Color is Colors.Secondary && !b.Disabled)
            {
                Class = "hover:bg-purple-600"
            },
        ]
    });

    private SlotMap<Slots> _slots = new();

    public enum Colors
    { Default, Primary, Secondary, Ghost }

    public enum Sizes
    { Small, Medium, Large }

    protected override void OnParametersSet()
    {
        _slots = _button(this, Tw);
    }

    private string? GetClasses() => _slots[b => b.Base];

    public sealed class Slots : ISlots
    {
        public string? Base { get; set; }
    }
}