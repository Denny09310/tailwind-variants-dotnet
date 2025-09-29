using Microsoft.CodeAnalysis;
using System.Text;

namespace TailwindVariants.NET.SourceGenerator;

internal static class SymbolHelper
{
    public static string MakeSafeFileName(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '.' || ch == '-') sb.Append(ch);
            else sb.Append('_');
        }
        return sb.ToString();
    }

    public static string MakeSafeIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name)) return "_";
        var sb = new StringBuilder(name.Length);
        for (int i = 0; i < name.Length; i++)
        {
            var ch = name[i];
            if (i == 0)
            {
                sb.Append(char.IsLetter(ch) || ch == '_' ? ch : '_');
            }
            else
            {
                sb.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_');
            }
        }
        return sb.ToString();
    }

    public static bool TryGetSlotMapArgument(ITypeSymbol type, INamedTypeSymbol? slotMapSymbol, out INamedTypeSymbol? slotsArg)
    {
        slotsArg = null;
        if (slotMapSymbol == null) return false;

        if (type is INamedTypeSymbol named && named.IsGenericType)
        {
            var original = named.OriginalDefinition;
            if (SymbolEqualityComparer.Default.Equals(original, slotMapSymbol))
            {
                if (named.TypeArguments.Length == 1 && named.TypeArguments[0] is INamedTypeSymbol t)
                {
                    slotsArg = t;
                    return true;
                }
            }
        }
        return false;
    }
}