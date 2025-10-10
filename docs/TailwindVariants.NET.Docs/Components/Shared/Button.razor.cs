namespace TailwindVariants.NET.Docs.Components.Shared;

public partial class Button : ISlotted<Button.Slots>
{
    public static readonly TvDescriptor<Button, Slots> _button = new
    (
        @base: "dummy",
        variants: new()
    );


    protected override TvDescriptor<Button, Slots> GetDescriptor() => _button;

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