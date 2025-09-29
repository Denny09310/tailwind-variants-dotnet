using System.Text;

namespace TailwindVariants.SourceGenerator;

internal class Indenter
{
    private const char Tab = '\t';
    private readonly StringBuilder _sb = new();
    private int _level = 0;

    public void Append(string text) => _sb.Append(GetIndentation()).Append(text);

    public void AppendLine(string text) => _sb.AppendLine(GetIndentation() + text);

    public void AppendLine() => _sb.AppendLine();

    public void Dedent() => _level = Math.Max(0, _level - 1);

    public void Indent() => _level++;

    public override string ToString() => _sb.ToString();

    private string GetIndentation() => new(Tab, _level);
}