using System.Linq.Expressions;
using TailwindVariants;
using static TailwindVariants.TvFunction;

using Card = Demo.Components.Shared.SimpleCard;

namespace Demo.Components.Shared;

public partial class SimpleCard
{
    private static readonly TvReturnType<Card, Slots> _card = Tv<Card, Slots>(new()
    {
        Slots = new()
        {
            [b => b.Base] = "md:flex bg-slate-100 rounded-xl p-8 md:p-0 dark:bg-gray-900",
            [b => b.Avatar] = "w-24 h-24 md:w-48 md:h-auto md:rounded-none rounded-full mx-auto drop-shadow-lg",
            [b => b.Wrapper] = "flex-1 pt-6 md:p-8 text-center md:text-left space-y-4",
            [b => b.Description] = "text-md font-medium",
            [b => b.InfoWrapper] = "font-medium",
            [b => b.Name] = "text-sm text-sky-500 dark:text-sky-400",
            [b => b.Role] = "text-sm text-slate-700 dark:text-slate-500",
        }
    });

    private SlotMap<Slots> _slots = new();

    protected override void OnParametersSet()
    {
        _slots = _card(this, Tw);
    }

    private string? GetClass(Expression<SlotAccessor<Slots>> accessor) => _slots[accessor];

    public sealed class Slots : ISlots
    {
        public string? Avatar { get; set; }
        public string? Base { get; set; }
        public string? Description { get; set; }
        public string? InfoWrapper { get; set; }
        public string? Name { get; set; }
        public string? Role { get; set; }
        public string? Wrapper { get; set; }
    }
}