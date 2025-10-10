namespace TailwindVariants.NET.Tests.Components;

public partial class IconButton : ISlotted<IconButton.Slots>
{
    private static readonly TvDescriptor<IconButton, Slots> _iconButton = new
    (
        extends: Button._button,
        slots: new()
        {
            [s => s.Base] = "inline-flex items-center",
            [b => b.Icon] = "mr-2 -ml-1",
        },
        variants: new()
        {
            [b => b.IconOnly] = new Variant<bool, Slots>()
            {
                [true] = new()
                {
                    [s => s.Base] = "aspect-square p-0",
                    [s => s.Icon] = "m-0"
                }
            }
        }
    );

    protected override TvDescriptor<IconButton, Slots> GetDescriptor() => _iconButton;

    public sealed partial class Slots : Button.Slots
    {
        public string? Icon { get; set; }
    }
}