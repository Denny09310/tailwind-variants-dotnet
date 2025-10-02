using System.Collections;

namespace TailwindVariants.NET;

/// <summary>
/// Represents a small wrapper over one or more CSS class fragments.
/// Allows implicit conversion from/to string.
/// </summary>
public class ClassValue : IEnumerable<string>
{
    private readonly string? _value;
    private List<string>? _values;

    /// <summary>
    /// Create an empty ClassValue.
    /// </summary>
    public ClassValue()
    { }

    /// <summary>
    /// Create a ClassValue from a single string.
    /// </summary>
    /// <param name="value">The class string.</param>
    public ClassValue(string value) => _value = value;

    /// <summary>
    /// Implicit conversion from string to ClassValue.
    /// </summary>
    public static implicit operator ClassValue(string value) => new(value);

    /// <summary>
    /// Implicit conversion from ClassValue to string.
    /// Will return the underlying string or the joined values.
    /// </summary>
    /// <exception cref="InvalidOperationException">Throws an informative if empty.</exception>
    public static implicit operator string(ClassValue @class)
    {
        if (!string.IsNullOrEmpty(@class._value))
        {
            return @class._value!;
        }

        if (@class._values is not null && @class._values.Count > 0)
        {
            return string.Join(" ", @class._values);
        }

        throw new InvalidOperationException($"{nameof(ClassValue)} does not contain any class data (neither a single value nor a collection).");
    }

    /// <summary>
    /// Add a single class fragment to the collection.
    /// </summary>
    /// <param name="value">A single class fragment.</param>
    public void Add(string value) => (_values ??= []).Add(value);

    /// <inheritdoc/>
    public IEnumerator<string> GetEnumerator()
    {
        if (_values is not null)
        {
            return _values.GetEnumerator();
        }

        if (!string.IsNullOrEmpty(_value))
        {
            return new List<string> { _value }.GetEnumerator();
        }

        throw new InvalidOperationException($"Cannot enumerate {nameof(ClassValue)} because it contains no values.");
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}