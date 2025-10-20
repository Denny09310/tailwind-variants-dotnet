# TwVariants

`TwVariants` is the **core function factory** in TailwindVariants.NET that builds a Tailwind-variants-like function. It is responsible for **computing the final CSS class strings** for each slot based on component variants, compound variants, and per-instance overrides.

---

## Overview

`TwVariants` provides a type-safe, compile-time-friendly way to map your **slots** and **variants** to CSS classes. It integrates seamlessly with:

- [Variants](docs/core-concepts/variants) — for strongly-typed variant definitions
- [Slots](docs/core-concepts/slots) — for defining named slots per component
- `ClassValue` — for combining multiple CSS fragments safely  

Using `TwVariants` ensures that your Blazor components are **maintainable, type-safe, and optimized**.

---

## Core Method: `Invoke`

```csharp
public SlotsMap<TSlots> Invoke<TOwner, TSlots>(
    TOwner owner, 
    TvDescriptor<TOwner, TSlots> definition
)
    where TSlots : ISlots, new()
    where TOwner : ISlottable<TSlots>
````

* **TOwner** — the component or object that owns the slots and variants (`ISlottable<TSlots>`).
* **TSlots** — the type representing your slots (`ISlots`).
* **definition** — a `TvDescriptor` describing base slots, variants, and compound variants.

**Returns:** A `SlotsMap<TSlots>` containing the computed CSS classes for each slot.

---

## How `Invoke` Works

1. **Start with base slots** from `definition.BaseSlots`.
2. **Apply variants** from `definition.BaseVariants`. Uses the selected property values to choose the correct CSS classes.
3. **Apply compound variants** (predicates based on multiple variant combinations).
4. **Apply per-instance overrides** (`owner.Classes`) if provided.
5. **Build the final `SlotsMap<TSlots>`** with merged CSS strings using `TailwindMerge`.

---

## Example Usage

```csharp
var buttonDescriptor = new TvDescriptor<Button, Button.Slots>(
    @base: new() { [b => b.Base] = "px-4 py-2 rounded" },
    variants: new()
    {
        [b => b.Variant] = new Variant<Button.Variants, Button.Slots>
        {
            [Button.Variants.Primary] = "bg-blue-600 text-white",
            [Button.Variants.Secondary] = "bg-gray-200 text-black"
        }
    }
);

var twVariants = new TwVariants(Tw.Merge);

var slotsMap = twVariants.Invoke(button, buttonDescriptor);

var baseClass = slotsMap.GetBase(); // "px-4 py-2 rounded bg-blue-600 text-white"
```

This safely **combines base, variant, and per-instance classes** without runtime string concatenation.

---

## Compound Variants

`TwVariants` supports [compound variants](variants#compound-variants):

* Predicates that trigger when **multiple variant values** are active
* Allows applying additional classes conditionally
* Safe and compile-time friendly

Example:

```csharp
var cvDescriptor = new TvDescriptor<Button, Button.Slots>(
    @base: "px-4 py-2 rounded",
    compoundVariants: new[]
    {
        new(b => b.Variant == Button.Variants.Primary && b.Disabled)
        {
            Class = "opacity-50 cursor-not-allowed"
        }
    }
);
```

---

## Benefits

* **Type-safe** access to CSS slots and variants
* **Compile-time generated helpers** (see [Source Generators](source-generators))
* **Supports per-instance overrides** (`Classes` property)
* **Compatible with TailwindMerge** for merging class strings

---

`TwVariants` is the **engine** behind strongly-typed Blazor components in TailwindVariants.NET.