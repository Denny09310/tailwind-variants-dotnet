using TailwindVariants.NET;

namespace Demo.Components.Shared;

public partial class Button : ISlotted<Button.Slots>
{
    public static readonly TvDescriptor<Button, Slots> _button = new
    (
        @base: "dummy",
        variants: new()
    );

    private SlotsMap<Slots> _slots = new();

    protected override void OnParametersSet()
    {
        _slots = Tv.Invoke(this, _button);
    }

    public enum Variants
    {
        Primary,
        Secondary,
        Danger
    }

    public sealed partial class Slots : ISlots
    {
        public string? Base { get; set; }
    }
}