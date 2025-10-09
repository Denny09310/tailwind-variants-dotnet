# TailwindVariants.NET

**TailwindVariants.NET** is a strongly typed Blazor library for managing **TailwindCSS variants** and slot-based styling.

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
* Supports .NET 8, .NET 9, and .NET Standard 2.0 (source generator only).
* Works seamlessly in Blazor projects (Server, WebAssembly, Hybrid).

---

## Installation

Install from NuGet:

```bash
dotnet add package TailwindVariants.NET
```

To enable automatic generation of slot accessors, it's included an analyzer with the main package:

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
using TailwindVariants.NET;

public partial class Button
{
    private static readonly TvDescriptor<Button, Slots> _button = new
    (
        @base: "font-semibold border rounded",
        variants: new()
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
        compoundVariants: 
        [
            new(b => b.Variant == Variants.Primary && !b.Disabled)
            {
                Class = "hover:bg-blue-600"
            }
        ]
    );

    private SlotsMap<Slots> _slots = new();

    protected override void OnParametersSet()
    {
        _slots = Tv.Invoke(this, _button);
    }

    public sealed partial class Slots : ISlots
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

This is enabled by the **incremental source generator** (`TailwindVariants.SourceGenerator`), which automatically generates accessors for each component that implements `ISlots`.

---

## How to Use in Blazor

1. Reference `TailwindVariants.NET` in your Blazor project.
3. Define your component's variants and slots using the provided API.
4. Use the generated slot accessors in your Razor markup for type-safe Tailwind class management.

---

## Acknowledgements / Credits

Special thanks to the authors of the following projects that are either used or inspired this work:

* [**tailwind-variants**](https://github.com/heroui-inc/tailwind-variants) ([jrgarciadev](https://github.com/jrgarciadev)) — for the general concept of variants & compound variants.
* [**tailwind-merge-dotnet**](https://github.com/desmondinho/tailwind-merge-dotnet) ([desmondinho](https://github.com/desmondinho)) — Tailwind class merge utilities.

Check out those projects for more tools and context.

---

## Contributing

Contributions are always welcome!

Please follow our [contributing guidelines](./CONTRIBUTING.md).

Please adhere to this project's [CODE_OF_CONDUCT](./CODE_OF_CONDUCT.md).

---

## License

MIT — see the `LICENSE` file in the repository.

---

## Repository & Issues

If you encounter problems or have feature requests, open an issue on the GitHub repository.
