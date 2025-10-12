namespace TailwindVariants.NET.Docs.Components.Shared;

public partial class Card : ISlotted<Card.Slots>
{
    private static readonly TvDescriptor<Card, Slots> _card = new
    (
        @base: "flex flex-col rounded-lg shadow-lg bg-white overflow-hidden dark:bg-zinc-500/20 gap-2 p-4 backdrop-blur",
        slots: new()
        {
            [s => s.Header] = "text-lg font-semibold text-neutral-900 dark:text-white",
            [s => s.Body] = "flex-1 text-neutral-700 dark:text-neutral-300",
            [s => s.Footer] = "text-sm text-neutral-500 dark:text-neutral-400 mt-4"
        }
    );

    protected override TvDescriptor<Card, Slots> GetDescriptor() => _card;

    public sealed partial class Slots : ISlots
    {
        public string? Base { get; set; }
        public string? Body { get; set; }
        public string? Footer { get; set; }
        public string? Header { get; set; }
    }
}