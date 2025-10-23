namespace TailwindVariants.NET;

/// <summary>
/// Associates a custom name with a slot property, typically for use in data attributes like `data-slot`.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SlotAttribute"/> class.
/// </remarks>
/// <param name="name">The custom name for the slot (e.g., "item-title").</param>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class SlotAttribute(string name) : Attribute
{
	/// <summary>
	/// The custom name for the slot.
	/// </summary>
	public string Name { get; } = name;
}
