# Source Generators

**TailwindVariants.NET** leverages **C# Source Generators** to provide **compile-time helpers** for slots and variants. This ensures safer, more efficient, and strongly-typed access to component styling.

---

## What are Source Generators?

Source Generators are a **Roslyn compiler feature** that can:

- Analyze your code at compile-time
- Generate additional C# code automatically
- Integrate seamlessly with your project without runtime overhead

In **TailwindVariants.NET**, they automatically generate:

- Strongly-typed **slot accessors**
- Helper classes and enums for **variants**
- Extension methods for getting classes

---

## Why Use Source Generators?

Without Source Generators, developers would have to rely on raw strings/expression for:

```csharp
var baseClass = _slots.Map["Base"];
var iconClass =  _slots.Map[s => s.Icon];
````

This is:

* ❌ Error-prone
* ❌ Not refactor-friendly
* ❌ Hard to maintain

With Source Generators, you can do:

```csharp
var baseClass = _slots.GetBase();
var iconClass = _slots.GetIcon();
```

* ✅ Compile-time safety
* ✅ Strongly-typed access
* ✅ Auto-completion in IDE

---

## How It Works

1. **Scans your component types** implementing `ISlots`.
2. **Collects public properties** that represent slots.
3. **Generates helper classes**:

   * `<ComponentName>SlotsNames` — array of slot names
   * `<ComponentName>SlotsExtensions` — extension methods for slots
   * `<ComponentName>SlotsTypes` — enum for variants
4. **Integrates with [TvDescriptor](docs/tv-descriptor) and [SlotCollection](docs/slots-collection)** automatically.

---

## Example

Suppose you have a `Button` component:

```csharp
public sealed partial class Slots : ISlots
{
    public string? Base { get; set; }
    public string? Icon { get; set; }
}
```

The Source Generator produces:

```csharp
public static class ButtonSlotsNames
{
    public static IReadOnlyList<string> AllNames => new[] { "Base", "Icon" };
    public static string NameOf(ButtonSlotsTypes key) => AllNames[(int)key];
}

public static class ButtonSlotsExtensions
{
    public static string? Get(this ButtonSlots slots, ButtonSlotsTypes key) 
        => slots.Map[ButtonSlotsNames.NameOf(key)];

    public static string? GetBase(this ButtonSlots slots) 
        => slots.Get(ButtonSlotsTypes.Base);

    public static string? GetIcon(this ButtonSlots slots) 
        => slots.Get(ButtonSlotsTypes.Icon);
}
```

This allows:

```csharp
var baseClass = buttonSlots.GetBase(); // type-safe access
var iconClass = buttonSlots.GetIcon();
```

---

## Benefits

* **No runtime string errors**
* **Refactor-safe code**
* **Better IDE support**
* **Performance optimized** — all logic happens at compile-time

---

## Best Practices

* Always declare your slots as **public properties** in `ISlots` classes.
* Keep your components **partial** so the generator can augment them.
* Use the generated **extension methods** instead of manually accessing `SlotCollection`.

---

**TailwindVariants.NET Source Generators** make your Blazor components **safer, easier to maintain, and fully integrated with your variant system**.
