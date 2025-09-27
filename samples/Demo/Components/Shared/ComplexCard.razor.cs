using System.Linq.Expressions;
using TailwindVariants;
using static TailwindVariants.TvFunction;

namespace Demo.Components.Shared;

public partial class ComplexCard : ISlotted<ComplexCard.Slots>
{
    private static readonly TvReturnType<ComplexCard, Slots> _card = Tv<ComplexCard, Slots>(new()
    {
        Slots = new()
        {
            [s => s.Base] = "flex flex-col mb-4 sm:flex-row p-6 bg-white dark:bg-stone-900 drop-shadow-xl rounded-xl",
            [s => s.ImageWrapper] = "flex-none w-full sm:w-48 h-48 mb-6 sm:mb-0 sm:h-auto relative z-10 before:absolute before:top-0 before:left-0 before:w-full before:h-full before:rounded-xl before:bg-[#18000E] before:bg-gradient-to-r before:from-[#010187]",
            [s => s.Img] = "sm:scale-125 absolute z-10 top-2 sm:left-2 inset-0 w-full h-full object-cover rounded-lg",
            [s => s.Title] = "relative w-full flex-none mb-2 text-2xl font-semibold text-stone-900 dark:text-white",
            [s => s.Price] = "relative font-semibold text-xl dark:text-white",
            [s => s.PreviousPrice] = "relative line-through font-bold text-neutral-500 ml-3",
            [s => s.PercentOff] = "relative font-bold text-green-500 ml-3",
            [s => s.SizeButton] = "cursor-pointer select-none relative font-semibold rounded-full w-10 h-10 flex items-center justify-center active:opacity-80 dark:text-white peer-checked:text-white",
            [s => s.BuyButton] = "text-xs sm:text-sm px-4 h-10 rounded-lg shadow-lg uppercase font-semibold tracking-wider text-white active:opacity-80",
            [s => s.AddToBagButton] = "text-xs sm:text-sm px-4 h-10 rounded-lg uppercase font-semibold tracking-wider border-2 active:opacity-80",
        },
        Variants = new()
        {
            [b => b.Color] = new Variant<Colors, Slots>
            {
                [Colors.Primary] = new()
                {
                    [s => s.BuyButton] = "bg-blue-500 shadow-blue-500/50",
                    [s => s.SizeButton] = "peer-checked:bg-blue",
                    [s => s.AddToBagButton] = "text-blue-500 border-blue-500",
                },
                [Colors.Secondary] = new()
                {
                    [s => s.BuyButton] = "bg-purple-500 shadow-purple-500/50",
                    [s => s.SizeButton] = "peer-checked:bg-purple",
                    [s => s.AddToBagButton] = "text-purple-500 border-purple-500",
                },
                [Colors.Success] = new()
                {
                    [s => s.BuyButton] = "bg-green-500 shadow-green-500/50",
                    [s => s.SizeButton] = "peer-checked:bg-green",
                    [s => s.AddToBagButton] = "text-green-500 border-green-500",
                },
            }
        }
    });

    private SlotMap<Slots> _slots = new();

    public enum Colors
    { Primary, Secondary, Success }

    protected override void OnParametersSet()
    {
        _slots = _card(this, Tw);
    }

    private string? GetClass(Expression<SlotAccessor<Slots>> accessor) => _slots[accessor];

    public sealed class Slots : ISlots
    {
        public string? AddToBagButton { get; set; }
        public string? Base { get; set; }
        public string? BuyButton { get; set; }
        public string? ImageWrapper { get; set; }
        public string? Img { get; set; }
        public string? PercentOff { get; set; }
        public string? PreviousPrice { get; set; }
        public string? Price { get; set; }
        public string? SizeButton { get; set; }
        public string? Title { get; set; }
    }
}