namespace TailwindVariants.NET.Models;

/// <summary>
/// Represents a Tailwind class string.
/// </summary>
/// <remarks>
/// Creates a new <see cref="ClassValue"/>.
/// </remarks>
public sealed class ClassValue(string value)
{
	/// <summary>
	/// Raw class string.
	/// </summary>
	public string Value { get; } = value;

	/// <summary>
	/// Implicit conversion from string.
	/// </summary>
	public static implicit operator ClassValue(string value)
		=> new(value);

	/// <inheritdoc />
	public override string ToString() => Value;
}
