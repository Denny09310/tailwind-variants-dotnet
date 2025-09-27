using System.Linq.Expressions;
using TailwindVariants;
using static TailwindVariants.TvFunction;

namespace Demo.Components.Shared;

public partial class Badge : ISlotted<Badge.Slots>
{
    private static readonly TvReturnType<Badge, Slots> _badge = Tv<Badge, Slots>(new()
    {
        Base = "inline-flex items-center rounded-full border border-transparent px-2.5 py-0.5 text-xs font-medium",
        Variants = new()
        {
            [b => b.Color] = new Variant<Colors, Slots>()
            {
                [Colors.Default] = "bg-slate-100 text-slate-800 dark:bg-slate-800 dark:text-slate-100",
                [Colors.Primary] = "bg-blue-100 text-blue-800 dark:bg-blue-800 dark:text-blue-100",
                [Colors.Secondary] = "bg-purple-100 text-purple-800 dark:bg-purple-800 dark:text-purple-100",
            },
            [b => b.Size] = new Variant<Sizes, Slots>()
            {
                [Sizes.Small] = "px-2.5 py-0.5 text-xs",
                [Sizes.Medium] = "px-3 py-0.5 text-sm",
                [Sizes.Large] = "px-3.5 py-1 text-sm",
            },
        }
    });

    private SlotMap<Slots> _slots = new();

    protected override void OnParametersSet()
    {
        _slots = _badge(this, Tw);
    }

    private string? GetSlot(Expression<SlotAccessor<Slots>> slot) => _slots[slot];

    public enum Colors
    { Default, Primary, Secondary, }

    public enum Sizes
    { Small, Medium, Large, }

    public sealed class Slots : ISlots
    {
        public string? Base { get; set; }
    }
}