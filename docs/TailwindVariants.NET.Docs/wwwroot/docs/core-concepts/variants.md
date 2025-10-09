# Variants

Variants are one of the core concepts in **TailwindVariants.NET**. They allow you to define reusable, strongly-typed variations for your components without relying on raw strings. Think of them as the “conditional classes” of your components — but safe, structured, and discoverable at compile time.

## Why Variants?

When building UI components, you often want to apply different styles depending on a property, state, or type. Traditionally, this leads to string concatenation or multiple `if` statements:

```csharp
var buttonClass = isPrimary ? "bg-blue-500 text-white" : "bg-gray-200 text-black";
````

With **Variants**, you can define these variations declaratively and safely:

* ✅ Strongly typed — the compiler ensures you only use valid variant values.
* ✅ Reusable — define once, apply everywhere.
* ✅ Easy to extend — new variants or values can be added without breaking existing code.
* ✅ Integrated with Source Generators — get compile-time helpers for your variants.

## Defining a Variant

Suppose you have a `Button` component with different visual styles:

```csharp
public enum Variants
{
    Primary,
    Secondary,
    Danger
}

public partial class Button : ISlotted<Button.Slots>
{
    public static readonly TvDescriptor<Button, Slots> _button = new(
        @base: "px-4 py-2 rounded font-medium",
        variants: new()
        {
            [b => b.Variant] = new Variant<Variants, Slots>()
            {
                [Variants.Primary] = "bg-blue-500 text-white",
                [Variants.Secondary] = "bg-gray-200 text-black",
                [Variants.Danger] = "bg-red-500 text-white"
            }
        }
    );

    private SlotsMap<Slots> _slots = new();
}
```

Here, the `Variant<Variants, Slots>` maps each `Variants` enum value to a CSS class string.

## Using Variants

Once defined, applying variants is simple:

```razor
<Button Variant="Variants.Primary">Primary</Button>
<Button Variant="Variants.Danger">Danger</Button>
```

The source generator automatically generates helpers to make this type-safe:

```csharp
_slots.GetBase()
```

You can also combine multiple variants (like size, color, or state) without manually concatenating strings.

## Best Practices

* Use enums for variant values — it guarantees type safety.
* Keep variants focused — one enum per dimension (e.g., `Size`, `Color`, `State`).
* Start with the most common variants first (`Primary`, `Secondary`) and expand as needed.
* Leverage the source-generated helpers — they reduce runtime errors and improve discoverability in IDEs.

---

Variants are the backbone of **TailwindVariants.NET**, enabling flexible, maintainable, and safe styling patterns for all your Blazor components.