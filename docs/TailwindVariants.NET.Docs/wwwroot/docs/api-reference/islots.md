# ISlots

`ISlots` is a **marker interface** that defines a type as a collection of named slots for a component.  
It works together with `SlotCollection<TSlots>` and [TwVariants](docs/api-reference/tw-variants) to provide **type-safe, strongly-typed class management**.

---

## Overview

- Each component defines a **slots class** implementing `ISlots`.  
- Slots represent **named CSS targets** in a component (e.g., `Base`, `Icon`, `Header`).  
- Enables **compile-time safety** for assigning, overriding, and merging CSS classes.

---

## Example

```csharp
public partial class Button : ISlottable<Button.Slots>
{
    public sealed partial class Slots : ISlots
    {
        public string? Base { get; set; }
        public string? Icon { get; set; }
    }

    public Slots Classes { get; set; } = new();
}
````

* Using `SlotCollection<TSlots>` with `ISlots`:

```csharp
var buttonSlots = new SlotCollection<Button.Slots>();
buttonSlots[b => b.Base] = "px-4 py-2 rounded";
buttonSlots[b => b.Icon] = "mr-2";
```

* Works with `TwVariants.Invoke` to generate final CSS classes:

```csharp
var slotsMap = twVariants.Invoke(button, buttonDescriptor);
var baseClass = slotsMap.GetBase();
```

---

## Benefits

* **Type-safe access** to CSS slots
* **Compatible with `SlotCollection` and `TwVariants`**
* Enables **strongly-typed component variants** in Blazor
* Integrates with **source-generated helpers** for compile-time safety