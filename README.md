# tailwind-variants-dotnet

**tailwind-variants-dotnet** is a strongly-typed Blazor library to manage **TailwindCSS variants** and slot-based styling.
It is *inspired* by libraries such as `tailwind-variants` (React ecosystem).

> ⚠️ This package is **not related** to `tailwind-variants` — it only takes inspiration from its ideas and applies them to a Blazor-first, strongly-typed API.

---

## :sparkles: Features

* Strongly-typed variant definitions for Blazor components (slotless and slot-aware).
* Per-slot default classes + slot-aware variants and compound variants.
* **Generated slot accessors** for convenient, strongly-typed access to slot classes (`b => b.Base` automatically wrapped).
* Simple builder API (`Variants<TOwner, TSlots>`).
* Integrates with `TailwindMerge` to resolve Tailwind class conflicts.
* Lightweight, no runtime JS required (Blazor/C# only).

---

## Installation

Install from NuGet:

```bash
dotnet add package tailwind-variants-dotnet
```

---

## Quick Examples

### Button (no slots)

```csharp
private static readonly VariantConfig<Button> _variants = new VariantBuilder<Button>()
  .Base("font-semibold border rounded")
  .Variant(b => b.Variant, new Dictionary<Variants, string>
  {
      [Variants.Primary] = "bg-blue-500 text-white border-transparent",
      [Variants.Secondary] = "bg-white text-gray-800 border-gray-400",
  })
  .Variant(b => b.Size, new Dictionary<Sizes, string>
  {
      [Sizes.Small] = "text-sm py-1 px-2",
      [Sizes.Medium] = "text-base py-2 px-4",
  })
  .Compound(b => b.Variant == Variants.Primary && !b.Disabled, "hover:bg-blue-600")
  .Build();
```

In the code-behind:
```csharp
private SlotsMap<Slots> _slots = new();

protected override void OnParametersSet()
{
    _slots = _variants.GetSlots(this, Tw);
}
```

In the component:

```razor
@inherits TailwindComponentBase

<button class="@Accessors.Base" disabled="@Disabled">
  @ChildContent
</button>

@code
{
    [Parameter]
    public Slots? Classes { get; set; }
}
```

#### Accessing Slots

With the **generated slot accessors**, you no longer need to write `_slots[b => b.Avatar]` manually. You can use strongly-typed properties:

```csharp
<img class="@Accessors.Avatar" src="avatar.png" />
<p class="@Accessors.Description">Description text</p>
```

This is made possible by the **incremental generator**, which automatically generates a `SlotsAccessors` class for each component implementing `ISlots`.

---

## Requirements

* .NET 8 or .NET 9
* TailwindCSS [standalone CLI](https://tailwindcss.com/blog/standalone-cli) (for styles)

---

## Thank you / Credits

This project draws inspiration from several excellent projects:

* **tailwind-variants** — for the overall idea of variants & compound variants.

Special thanks to the maintainers/authors of these projects which are used or inspired this work:

* [**tailwind-merge-dotnet**](https://github.com/desmondinho/tailwind-merge-dotnet) — for Tailwind class merging utilities.
* [**BlazorComponentUtilities**](https://github.com/EdCharbeneau/BlazorComponentUtilities) — for helpful Blazor CSS builder utilities.

Please consult those projects for additional tooling and context.

---

## License

MIT — see the `LICENSE` file in the repository.

---

## Repository & Issues

If you find issues or have feature requests, please open them on the GitHub repository.