using TailwindVariants.NET;

namespace Demo.Components.Shared;

public partial class Textbox : ISlotted<Textbox.Slots>
{
    private static readonly TvOptions<Textbox, Slots> _textbox = new
    (
        @base: "block w-full rounded-md border bg-white py-2 px-3 text-sm placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-neutral-400 disabled:opacity-50 disabled:pointer-events-none dark:bg-neutral-900 dark:border-neutral-700 dark:placeholder:text-neutral-500",
        slots: new()
        {
            [t => t.InnerWrapper] = "relative flex items-center",
            [t => t.OuterWrapper] = "flex flex-col gap-1",
            [t => t.Prefix] = "mr-2 inline-flex items-center",
            [t => t.Suffix] = "ml-2 inline-flex items-center",
            [t => t.Label] = "text-sm font-medium text-neutral-700 dark:text-neutral-200",
            [t => t.Error] = "mt-1 text-sm text-red-600"
        },
        variants: new()
        {
            [t => t.Variant] = new Variant<Variants, Slots>()
            {
                [Variants.Outline] = "border border-neutral-200 dark:border-neutral-700",
                [Variants.Solid] = "bg-neutral-50 border border-transparent dark:bg-neutral-800",
                [Variants.Ghost] = "bg-transparent border border-transparent",
                [Variants.Underline] = "border-b rounded-none py-1 px-0"
            },
            [t => t.Size] = new Variant<Sizes, Slots>()
            {
                [Sizes.Small] = new()
                {
                    [s => s.Base] = "h-8 px-2 text-sm",
                    [s => s.Prefix] = "mr-1",
                    [s => s.Suffix] = "ml-1"
                },
                [Sizes.Medium] = new()
                {
                    [s => s.Base] = "h-10 px-3 text-base"
                },
                [Sizes.Large] = new()
                {
                    [s => s.Base] = "h-12 px-4 text-lg"
                }
            },
            [t => t.Disabled] = new Variant<bool, Slots>()
            {
                [true] = new() { [s => s.Base] = "opacity-50 cursor-not-allowed" }
            },
            [t => t.FullWidth] = new Variant<bool, Slots>()
            {
                [true] = new() { [s => s.OuterWrapper] = "w-full" }
            },
            [t => t.ReadOnly] = new Variant<bool, Slots>()
            {
                [true] = new() { [s => s.Base] = "bg-neutral-50 dark:bg-neutral-800" }
            },
            [t => t.Invalid] = new Variant<bool, Slots>()
            {
                // "Invalid" is a helper variant added below (see property below)
                [true] = new()
                {
                    [s => s.Base] = "border-red-500 ring-red-500 focus:ring-red-500",
                    [s => s.Error] = "block"
                }
            }
        },

        // Compound variants: special cases combining variant + size + invalid
        compoundVariants:
        [
            // Small + Solid -> slightly reduced padding for compact appearance
            new(b => b.Size == Sizes.Small && b.Variant == Variants.Solid)
            {
                [s => s.OuterWrapper] = "py-1 px-2"
            },

            // Outline + Invalid -> stronger red ring + drop shadow
            new(b => b.Invalid && b.Variant == Variants.Outline)
            {
                [s => s.OuterWrapper] = "ring-1 ring-red-500 shadow-sm"
            },

            // Underline + Large -> increase underline thickness (example)
            new(b => b.Size == Sizes.Large && b.Variant == Variants.Underline)
            {
                [s => s.OuterWrapper] = "border-b-2"
            }
        ]
    );

    private SlotsMap<Slots> _slots = new();

    public enum Sizes
    { Small, Medium, Large }

    public enum Variants
    { Outline, Solid, Ghost, Underline }

    // Add an "Invalid" property to be used by the styling engine (compound/variants)
    // This property is not a Parameter on purpose — it's derived from Error presence.
    private bool Invalid => !string.IsNullOrEmpty(Error);

    protected override void OnParametersSet()
    {
        // When parameters change recompute the slots map (uses the current parameter values)
        // Note: we set the helper/derived properties via closure above (Invalid).
        _slots = Tv.Invoke(this, _textbox);
    }

    // Slots that can be overridden via the Classes parameter
    public sealed partial class Slots : ISlots
    {
        public string? Base { get; set; }
        public string? Error { get; set; }
        public string? OuterWrapper { get; set; }
        public string? Label { get; set; }
        public string? Prefix { get; set; }
        public string? Suffix { get; set; }
        public string? InnerWrapper { get; set; }
    }
}