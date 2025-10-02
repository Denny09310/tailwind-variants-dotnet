using TailwindVariants.NET;

namespace Demo.Components.Shared;

public partial class Card : ISlotted<Card.Slots>
{
    private static readonly TvOptions<Card, Slots> _card = new
    (
        @base: "flex flex-col rounded-lg shadow-lg bg-white overflow-hidden dark:bg-zinc-500/20 gap-2 p-4 backdrop-blur",
        slots: new()
        {
            [s => s.Header] = "text-lg font-semibold text-gray-900 dark:text-white",
            [s => s.Body] = "flex-1 text-gray-700 dark:text-gray-300",
            [s => s.Footer] = "text-sm text-gray-500 dark:text-gray-400 mt-4"
        }
    );

    private SlotsMap<Slots> _slots = new();

    protected override void OnParametersSet()
    {
        _slots = Tv.Invoke(this, _card);
    }

    public sealed partial class Slots : ISlots
    {
        public string? Base { get; set; }
        public string? Header { get; set; }
        public string? Body { get; set; }
        public string? Footer { get; set; }
    }
}