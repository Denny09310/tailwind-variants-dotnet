namespace TailwindVariants.NET;

/// <summary>
/// Represents an element or component that exposes a CSS class string.
/// </summary>
public interface IStyleable
{
	/// <summary>
	/// Gets or sets the CSS class string applied to the element or component.
	/// </summary>
	string? Class { get; set; }
}
