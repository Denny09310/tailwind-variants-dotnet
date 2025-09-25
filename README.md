# tailwind-variants-dotnet

**tailwind-variants-dotnet** is a strongly-typed Blazor library to manage **TailwindCSS variants** and slot-based styling. 
It is *inspired* by libraries such as `tailwind-variants` (React ecosystem)

> ⚠️ This package is **not related** to `tailwind-variants` — it only takes inspiration from its ideas and applies them to a Blazor-first, strongly-typed API.

## :sparkles: Features
- Strongly-typed variant definitions for Blazor components (slotless and slot-aware).
- Per-slot default classes + slot-aware variants and compound variants.
- Simple builder API (`VariantBuilder<T>` and `VariantBuilder<T, TSlots>`).
- Integrates with `TailwindMerge` to resolve Tailwind class conflicts.
- Lightweight, no runtime JS required (Blazor/C# only).

## Installation

Install from NuGet:

```bash
dotnet add package tailwind-variants-dotnet
````

## Quick examples

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

In the component:

```razor
<button class="@_variants.GetClasses(this, Tw, Class)" disabled="@Disabled">
  @ChildContent
</button>
```

### Card (with slots)

```csharp
private static readonly VariantConfig<Card, Card.Slots> _variants = new VariantBuilder<Card, Card.Slots>()
  .Base("md:flex bg-slate-100 rounded-xl p-8")
  .Slot(s => s.Avatar, "w-24 h-24 rounded-full")
  .Slot(s => s.Description, "text-md font-medium")
  .Build();
```

In the component:

```razor
<img class="@_variants.GetSlot(this, Tw, s => s.Avatar)" src="avatar.png" />
<p class="@_variants.GetSlot(this, Tw, s => s.Description)">...</p>
```

## Requirements

* .NET 8 or .NET 9
* TailwindCSS [standalone cli](https://tailwindcss.com/blog/standalone-cli) (for styles)

## Thank you / Credits

This project draws inspiration from several excellent projects:

* **tailwind-variants** — for the overall idea of variants & compound variants.

Special thanks to the maintainers/authors of these projects which are used or inspired this work:

* [**tailwind-merge-dotnet**](https://github.com/desmondinho/tailwind-merge-dotnet) — for Tailwind class merging utilities.
* [**BlazorComponentUtilities**](https://github.com/EdCharbeneau/BlazorComponentUtilities) — for helpful Blazor CSS builder utilities.

Please consult those projects for additional tooling and context.

## License

MIT — see the `LICENSE` file in the repository.

## Repository & Issues

If you find issues or have feature requests, please open them on the GitHub repository.
