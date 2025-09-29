# TailwindVariants.NET

**TailwindVariants.NET** is a strongly typed Blazor library for managing **TailwindCSS variants** and slot-based styling.
It takes inspiration from libraries like `tailwind-variants` (in the React ecosystem).

> ⚠️ This package is **not related** to `tailwind-variants` — it only draws inspiration from its ideas and applies them to a strongly typed, Blazor-first API.

---

## :sparkles: Features

* Strongly typed variant definitions for Blazor components (with or without slots).
* Default classes for slots + variants, and slot-aware compound variants.
* **Generated slot accessors** for convenient, strongly typed access to slot classes (`b => b.Base` automatically wrapped).
* Simple API builder (`Variants<TOwner, TSlots>`).
* Integration with `TailwindMerge.NET` to resolve conflicts between Tailwind classes.
* Lightweight, no JS runtime dependencies (pure Blazor/C#).
* **Incremental source generator** (`TailwindVariants.SourceGenerator`) for generating strongly typed slot accessors.

---

## Installation

Install from NuGet:

```bash
dotnet add package TailwindVariants.NET
```

To enable automatic generation of slot accessors, also add the source generator package:

```bash
dotnet add package TailwindVariants.NET.SourceGenerator
```

Both packages support:

* .NET 8
* .NET 9
* .NET Standard 2.0 (generator only)

---

## Quick Examples

### Button (without additional slots)

```csharp
using TailwindVariants;
using static TailwindVariants.TvFunction;

public static class Button
{
    private static readonly TvReturnType<Button, Slots> _button = Tv<Button, Slots>(new()
    {
        Base = "font-semibold border rounded",
        Variants = new()
        {
            [b => b.Variant] = new Variant<Variants, Slots>()
            {
                [Variants.Primary] = "bg-blue-500 text-white border-transparent",
                [Variants.Secondary] = "bg-white text-gray-800 border-gray-400",
            },
            [b => b.Size] = new Variant<Sizes, Slots>()
            {
              [Sizes.Small] = "text-sm py-1 px-2",
              [Sizes.Medium] = "text-base py-2 px-4",
            },
        }
        CompoundVariants = 
        [
            new(b => b.Variant == Variants.Primary && !b.Disabled)
            {
                Class = "hover:bg-blue-600"
            }
        ]
    });

    private SlotMap<Slots> _slots = new();

    protected override void OnParametersSet()
    {
        _slots = _button(this, Tw);
    }

    public sealed class Slots : ISlots
    {
        public string? Base { get; set; }
    }

    // ... enums omitted for brevity ...
}
```

In the component:

```razor
@inherits TailwindComponentBase

<button class="@_slots.GetBase()" disabled="@Disabled">
  @ChildContent
</button>

@code
{
    [Parameter]
    public Variants Variant { get; set; }

    [Parameter]
    public Sizes Size { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public Slots? Classes { get; set; }
}
```

#### Slot Access

With **generated slot accessors** (via the source generator), you no longer need to write `_slots[b => b.Avatar]` manually.
You can use strongly typed properties:

```csharp
<img class="@_slots.GetBase()" src="avatar.png" />
<p class="@_slots.GetDescription()">Description text</p>
```

This is enabled by the **incremental source generator** (`TailwindVariants.SourceGenerator`), which automatically generates a `SlotsAccessors` class for each component that implements `ISlots`.

---

## Requirements

* .NET 8 or .NET 9 (Blazor)
* .NET Standard 2.0 (generator only)
* TailwindCSS [standalone CLI](https://tailwindcss.com/blog/standalone-cli) (for styles)

---

## Acknowledgements / Credits

This project draws inspiration from several excellent projects:

* **tailwind-variants** — for the general concept of variants & compound variants.

Special thanks to the authors/maintainers of the following projects that are either used or inspired this work:

* [**tailwind-merge-dotnet**](https://github.com/desmondinho/tailwind-merge-dotnet) — Tailwind class merge utilities.
* [**BlazorComponentUtilities**](https://github.com/EdCharbeneau/BlazorComponentUtilities) — Blazor CSS builder utilities.

Check out those projects for more tools and context.

---

## License

MIT — see the `LICENSE` file in the repository.

---

## Repository & Issues

If you encounter problems or have feature requests, open an issue on the GitHub repository.