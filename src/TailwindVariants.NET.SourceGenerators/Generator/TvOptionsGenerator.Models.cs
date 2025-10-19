using System.Text;

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TailwindVariants.NET.SourceGenerators;

public partial class TvOptionsGenerator
{
	private readonly record struct InheritanceInfo(
		bool IsDirectImplementation,
		string? BaseClassName);

	private readonly record struct OptionsInfo(
		string FullName,
		string ExtClassName,
		string SlotsClassName,
		string EnumClassName,
		string NamesClassName,
		string OptionsClassName,
		string NamespaceName,
		string SlotsTypeName,
		InheritanceInfo Inheritance,
		EquatableArray<string> SlotsProperties,
		EquatableArray<(string Name, string Type)> VariantsProperties);

	public sealed class InMemoryRazorProjectItem(string path, string content) : RazorProjectItem
	{
		public override string BasePath => "/";
		public override bool Exists => true;
		public override string FilePath => path;
		public override string PhysicalPath => null!;
		public override string RelativePhysicalPath => path;

		public override Stream Read() => new MemoryStream(Encoding.UTF8.GetBytes(content));
	}

	private class DescriptorComparer : IEqualityComparer<DescriptorInfo>
	{
		public static DescriptorComparer Instance { get; } = new();

		public bool Equals(DescriptorInfo x, DescriptorInfo y)
			=> SymbolEqualityComparer.Default.Equals(x.Component, y.Component)
			   && SymbolEqualityComparer.Default.Equals(x.Slots, y.Slots);

		public int GetHashCode(DescriptorInfo obj)
		{
			unchecked
			{
				var h1 = SymbolEqualityComparer.Default.GetHashCode(obj.Component);
				var h2 = SymbolEqualityComparer.Default.GetHashCode(obj.Slots);
				return (h1 * 397) ^ h2;
			}
		}
	}

	private record struct DescriptorInfo(ArgumentListSyntax? ArgumentList, INamedTypeSymbol Component, INamedTypeSymbol Slots);
}
