using System.Text.RegularExpressions;

namespace TailwindVariants.NET.Docs.Components.Docs.Pages
{
    public partial class Router
    {
        [GeneratedRegex(@"\b\w")]
        private static partial Regex GetWordBoundaries();
    }
}