using TailwindVariants;
using static TailwindVariants.TvFunction;

namespace Demo.Components.Shared;

public partial class Button
{
    private static readonly TvReturnType<Button, Slots> _variants = Tv<Button, Slots>(new()
    {
        Base = "font-medium rounded-full active:opacity-80",
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
            }
        },
        CompoundVariants =
        [
            new(b => b.Size is Sizes.Small or Sizes.Medium)
            {
                Class = "px-3 py-1"
            }
        ]
    });

    public enum Colors
    { Default, Primary, Secondary, Ghost }

    public enum Sizes
    { Small, Medium, Large }

    public sealed class Slots : ISlots
    {
        public string? Base { get; set; }
    }

    private string? GetClasses() => _variants(this, Tw);
}