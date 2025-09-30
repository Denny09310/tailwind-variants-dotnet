using Microsoft.CodeAnalysis;

namespace TailwindVariants.NET.SourceGenerator;

internal class DiagnosticHelper
{
    public static readonly DiagnosticDescriptor NoPropertiesDescriptor = new(
        id: "TVSG001",
        title: "Slots type contains no public instance properties",
        messageFormat: "The slots type '{0}' contains no public instance properties. No extension methods will be generated.",
        category: "TailwindVariants.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);
}
