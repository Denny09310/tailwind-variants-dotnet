namespace TailwindVariants.NET.SourceGenerators;

public partial class SlotsAccessorGenerator
{
	private readonly record struct InheritanceInfo(
		bool IsDirectImplementation,
		string? BaseClassName);

	private readonly record struct SlotsAccessorToGenerate(
		string Name,
		string FullName,
		string ComponentFullName,
		string TypeName,
		string NamespaceName,
		string Modifiers,
		string SlotsMapName,
		string EnumName,
		string ExtClassName,
		string NamesClass,
		string? BaseClassName,
		bool IsDirectImplementation,
		bool IsSealed,
		bool IsNested,
		bool IsGetNameImplemented,
		EquatableArray<(string Name, string Modifiers)> Hierarchy,
		EquatableArray<(string Name, string Slot)> Properties,
		EquatableArray<string> AllProperties);
}
