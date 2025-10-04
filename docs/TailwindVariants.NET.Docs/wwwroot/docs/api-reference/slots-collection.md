# SlotCollection

`SlotCollection<TSlots>` is a **mapping of slot accessors to CSS class values**. It’s used internally and externally to safely store, merge, and enumerate CSS classes per slot.

---

## Overview

- Provides **type-safe access** to slots of a component (`TSlots : ISlots`).  
- Supports **implicit conversion from string**, which becomes the `Base` slot.  
- Can store **multiple CSS fragments** for a slot via `ClassValue`.  
- Implements `IEnumerable<KeyValuePair<Expression<SlotAccessor<TSlots>>, ClassValue>>` for iteration.

---

## Key Features

- **Indexer by SlotAccessor**:

```csharp
var classes = new SlotCollection<Button.Slots>();
classes[b => b.Base] = "px-4 py-2 rounded";
classes[b => b.Icon] = "mr-2";
````

* **Implicit string conversion**:

```csharp
SlotCollection<Button.Slots> baseSlot = "px-4 py-2 rounded"; // becomes Base slot
```

* **Adding multiple classes**:

```csharp
classes.Add("bg-blue-600 text-white"); // adds to Base slot
```

* **Enumeration**:

```csharp
foreach (var kv in classes)
{
    Console.WriteLine($"{kv.Key}: {kv.Value}");
}
```

---

## Integration

* Works seamlessly with [TwVariants](docs/api-reference/tw-variants) for computing final class strings
* Supports **per-instance overrides** via the `Classes` property of `ISlotted<TSlots>` components

---

## Example

```csharp
var slotCollection = new SlotCollection<Button.Slots>();
slotCollection[b => b.Base] = "px-4 py-2 rounded";
slotCollection[b => b.Icon] = "mr-2";

// Implicit string conversion
SlotCollection<Button.Slots> baseOnly = "text-sm font-medium";

foreach (var kv in slotCollection)
{
    Console.WriteLine($"{kv.Key} => {kv.Value}");
}
```
---

`SlotCollection` is the **core storage mechanism** for strongly-typed component slots in TailwindVariants.NET.