using System.Collections;

namespace TailwindVariants.NET;

/// <summary>
/// Represents a small wrapper over one or more CSS class fragments.
/// Allows implicit conversion from/to string.
/// </summary>
public class ClassValue() : IEnumerable<string>
{
	private List<string>? _values;

	/// <summary>
	/// Create a ClassValue from a single string.
	/// </summary>
	/// <param name="value">The class string.</param>
	public ClassValue(string? value) : this() => Add(value);

	/// <summary>
	/// Implicit conversion from string to ClassValue.
	/// </summary>
	public static implicit operator ClassValue(string? value) => new(value);

	/// <summary>
	/// Add a single class fragment to the collection.
	/// </summary>
	/// <param name="value">A single class fragment.</param>
	public void Add(string? value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return;
		}

		(_values ??= []).Add(value);
	}

	/// <inheritdoc/>
	public IEnumerator<string> GetEnumerator()
		=> _values?.GetEnumerator() ?? Enumerable.Empty<string>().GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();

	/// <summary>
	/// Inserts a single class fragment to the collection at the specified index
	/// </summary>
	/// <param name="index">The index in which to inser the class.</param>
	/// /// <param name="value">A single class fragment.</param>
	public void Insert(int index, string? value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return;
		}

		(_values ??= []).Insert(index, value);
	}

	/// <summary>
	/// Conversion from ClassValue to string.
	/// Will return the underlying string or the joined values.
	/// </summary>
	public override string ToString()
		=> _values is not null && _values.Count > 0
			? string.Join(" ", _values.Where(v => !string.IsNullOrWhiteSpace(v)))
			: string.Empty;
}
