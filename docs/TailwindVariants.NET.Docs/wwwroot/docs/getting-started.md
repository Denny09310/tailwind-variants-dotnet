## Getting Started

TailwindVariants.NET brings the power of [Tailwind CSS](https://tailwindcss.com) and the composability of **variants** to Blazor.  
It helps you create reusable, type-safe, and expressive UI components.

---

## Installation

Install the package via NuGet:

```bash
dotnet add package TailwindVariants.NET
````

That’s all you need. The source generator and base utilities are included in a single package — no extra analyzers required.

---

## Quick Example

Let’s create a simple `Button` component using `TailwindVariants.NET`.

### Button.razor

```razor
@inherits TwComponentBase

<button class="@_slots.GetBase()" @attributes="AdditionalAttributes">
    @ChildContent
</button>

@code 
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public Slots? Classes { get; set; }

    [Parameter] public Variants Variant { get; set; } = Variants.Primary;
}
```

### Button.razor.cs

```csharp
using TailwindVariants.NET;

namespace Demo.Components.Shared;

public partial class Button : ISlotted<Button.Slots>
{
    public static readonly TvOptions<Button, Slots> _button = new
    (
        @base: "px-4 py-2 font-semibold rounded-lg",
        variants: new()
        {
            [b => b.Variant] = new Variant<Variants, Slots>()
            {
                [Variants.Primary] = "bg-sky-600 text-white hover:bg-sky-700",
                [Variants.Secondary] = "bg-gray-200 text-gray-800 hover:bg-gray-300",
                [Variants.Danger] = "bg-red-600 text-white hover:bg-red-700"
            }
        }
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
```

---

## Usage

Now you can use the `Button` in your Blazor pages:

```razor
<Button Variant="Button.Variants.Primary">Save</Button>
<Button Variant="Button.Variants.Secondary">Cancel</Button>
<Button Variant="Button.Variants.Danger">Delete</Button>
```

---

That’s it! You now have a type-safe, Tailwind-powered Blazor component. 🎉