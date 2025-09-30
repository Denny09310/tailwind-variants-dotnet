namespace TailwindVariants.NET;

internal class CssBuilder()
{
    private readonly HashSet<string> _classes = [];

    public CssBuilder(string? classes) : this()
    {
        AddClass(classes);
    }

    public void AddClass(string? className)
    {
        if (!string.IsNullOrWhiteSpace(className))
        {
            _classes.Add(className);
        }
    }

    public string Build() => _classes.Count == 0 ? string.Empty : string.Join(" ", _classes);
}