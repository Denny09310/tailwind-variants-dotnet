# ClassValue

**ClassValue** is a helper type in **TailwindVariants.NET** that represents one or more CSS classes in a flexible, strongly-typed way. It works hand-in-hand with **slots** and **variants** to make class management safe, reusable, and composable.

---

## Why ClassValue?

When building components, you often end up with multiple CSS strings that need to be combined dynamically:

```csharp
var classes = "px-4 py-2 rounded bg-blue-500 text-white";
````

Managing these strings manually can get messy, especially when applying **variants** or **conditional classes**. `ClassValue` abstracts this:

* ✅ Can hold **single or multiple classes**
* ✅ Implicit conversion to/from string
* ✅ Enumerable for combining multiple class fragments
* ✅ Works naturally with **SlotCollection** and **TvDescriptor**

---

## Creating a ClassValue

You can create a `ClassValue` directly from a string:

```csharp
ClassValue primary = "bg-blue-500 text-white";
```

Or by combining multiple fragments:

```csharp
ClassValue combined = new();
combined.Add("px-4 py-2");
combined.Add("rounded");
combined.Add("bg-blue-500 text-white");
```

You can also use it implicitly with slots:

```csharp
public sealed partial class Slots : ISlots
{
    public string? Base { get; set; }
}

_slots[s => s.Base] = "px-4 py-2 bg-blue-500 rounded";
```

---

## Using ClassValue with Slots and Variants

`ClassValue` integrates directly with slots and variants:

```csharp
public static readonly TvDescriptor<Button, Slots> _button = new(
    @base: "px-4 py-2 rounded",
    variants: new()
    {
        [b => b.Variant] = new Variant<Variants, Slots>()
        {
            [Variants.Primary] = new Slots() { Base = "bg-blue-500 text-white" },
            [Variants.Secondary] = new Slots() { Base = "bg-gray-200 text-black" }
        }
    }
);
```

The generated `_slots` object will correctly combine the base slot with any variant-specific `ClassValue`, producing the final class string for the component.

---

## Key Features

* **Implicit string conversion**
  You can assign a string directly to a `ClassValue` and vice versa.

* **Composable**
  Add multiple class fragments dynamically and safely.

* **Enumerable**
  Iterate over each class in a `ClassValue` if needed.

* **Slot-aware**
  Works seamlessly with `SlotCollection` for strongly-typed component styling.

---

## Best Practices

* Prefer using `_slots.Get[SlotName]()` in components instead of manually concatenating strings.
* Combine `ClassValue` fragments for conditional styles instead of building raw strings.
* Leverage source generators — they provide compile-time helpers to reduce runtime errors.

---

With **ClassValue**, your Tailwind classes in Blazor components become **type-safe, composable, and maintainable**, forming a core piece of the **TailwindVariants.NET** approach.
