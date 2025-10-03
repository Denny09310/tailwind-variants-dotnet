# First Component

In this guide, we’ll build our first styled component using **TailwindVariants.NET**.
The library helps you organize Tailwind CSS classes in a clean, composable, and conflict-aware way.

Instead of writing long `class=""` strings directly in your `.razor` files, you define styles using:

* **Base classes** – default styling for your component.
* **Slots** – named sections of your component that can each have their own styles.
* **Variants** – conditional styling that changes depending on parameters.

---

## Step 1: Create a base component

To make this easy, you can define a base class that all styled components inherit from. This integrates Tailwind Merge and manages slot mappings automatically:

```csharp
using Microsoft.AspNetCore.Components;

namespace TailwindVariants.NET.Docs.Components.Shared;

public abstract class TwComponentBase<TOwner, TSlots> : ComponentBase
    where TSlots : ISlots, new()
    where TOwner : ISlotted<TSlots>
{
    protected SlotsMap<TSlots> _slots = new();

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Inject]
    protected TwVariants Tv { get; set; } = default!;

    protected abstract TvDescriptor<TOwner, TSlots> GetDescriptor();

    protected override void OnParametersSet()
    {
        if (this is TOwner owner)
        {
            _slots = Tv.Invoke(owner, GetDescriptor());
        }

        base.OnParametersSet();
    }
}
```

This base component will handle merging class names and expose `_slots` to access styles for each part of the component.

---

## Step 2: Define your component

Let’s build a **Button** component. Start with the Razor markup:

```razor
@inherits TwComponentBase<Button, Button.Slots>

<button class="@_slots.GetBase()" @attributes="AdditionalAttributes">
    @ChildContent
</button>

@code {
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public Slots? Classes { get; set; }

    [Parameter] public Colors Color { get; set; } = Colors.Primary;
    [Parameter] public Sizes Size { get; set; } = Sizes.Medium;
    [Parameter] public bool Disabled { get; set; }
}
```

Notice how the `class` attribute is set to `@_slots.GetBase()`.
This means the actual Tailwind classes come from the `TvDescriptor` (see next step).

---

## Step 3: Add the descriptor

In the `.razor.cs` file, define how the component should look:

```csharp
namespace TailwindVariants.NET.Docs.Components.Shared;

public partial class Button : ISlotted<Button.Slots>
{
    private static readonly TvDescriptor<Button, Slots> _button = new
    (
        @base: "inline-flex items-center justify-center font-medium rounded transition-colors focus:outline-none",
        variants: new()
        {
            [b => b.Color] = new Variant<Colors, Slots>
            {
                [Colors.Primary] = "bg-sky-600 text-white hover:bg-sky-700",
                [Colors.Secondary] = "bg-gray-200 text-black hover:bg-gray-300",
                [Colors.Danger] = "bg-red-600 text-white hover:bg-red-700"
            },
            [b => b.Size] = new Variant<Sizes, Slots>
            {
                [Sizes.Small] = "px-2 py-1 text-sm",
                [Sizes.Medium] = "px-4 py-2 text-base",
                [Sizes.Large] = "px-6 py-3 text-lg"
            },
            [b => b.Disabled] = new Variant<bool, Slots>
            {
                [true] = "opacity-50 cursor-not-allowed"
            }
        }
    );

    protected override TvDescriptor<Button, Slots> GetDescriptor() => _button;

    public enum Colors { Primary, Secondary, Danger }
    public enum Sizes { Small, Medium, Large }

    public sealed partial class Slots : ISlots
    {
        public string? Base { get; set; }
    }
}
```

---

## Step 4: Use it in a page

```razor
<Button Color="Button.Colors.Primary">Save</Button>
<Button Color="Button.Colors.Secondary" Size="Button.Sizes.Small">Cancel</Button>
<Button Color="Button.Colors.Danger" Disabled="true">Delete</Button>
```

The output buttons will be styled with Tailwind classes defined in your descriptor.
Variants apply conditionally, and Tailwind Merge ensures no duplicate or conflicting classes.

---

## How it works

* **`TvDescriptor`** defines your styling in one place.
* **Variants** are mapped to component parameters (e.g., `Color`, `Size`, `Disabled`).
* **Slots** allow breaking styles into different named areas (e.g., `Base`, `Icon`, `Wrapper`).
* **TwComponentBase** automatically merges everything and makes the final `class` available to your markup.

This pattern scales well as components grow in complexity. You can use it for links, navbars, sidebars, cards, modals—anything that needs structured styling.

---

✅ You’ve built your first component using **TailwindVariants.NET**. Next, try extending it with **slots** for extra parts (like an icon inside the button), or experiment with **dynamic variants** for things like `IsLoading`.
