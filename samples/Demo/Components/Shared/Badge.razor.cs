using TailwindVariants.NET;

namespace Demo.Components.Shared;

public partial class Badge : ISlotted<Badge.Slots>
{
    private static readonly TvOptions<Badge, Slots> _badge = new
    (
        @base: "inline-flex items-center rounded-full border border-transparent px-2.5 py-0.5 text-xs font-medium",
        variants: new()
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
    );

    private SlotsMap<Slots> _slots = new();

    protected override void OnParametersSet()
    {
        _slots = Tv.Invoke(this, _badge);
    }

    public enum Colors
    { Default, Primary, Secondary, }

    public enum Sizes
    { Small, Medium, Large, }

    public sealed class Slots : ISlots
    {
        public string? Base { get; set; }
    }
}