using System.Collections.Immutable;

namespace TailwindVariants.NET.SourceGenerators;

public partial class SlotsAccessorGenerator
{
	private static void WriteEnum(Indenter sb, SlotsAccessorToGenerate accessor)
	{
		sb.AppendLine();
		sb.AppendLine($"public enum {accessor.EnumName}");
		sb.AppendLine("{");
		sb.Indent();
		// Use AllProperties to include inherited properties
		ImmutableArray<string> properties = accessor.AllProperties;
		for (int i = 0; i < properties.Length; i++)
		{
			var nm = SymbolHelper.MakeSafeIdentifier(properties[i]);
			sb.AppendLine($"{nm} = {i},");
		}
		sb.Dedent();
		sb.AppendLine("}");
		sb.AppendLine();
	}

	private static void WriteExtensions(Indenter sb, SlotsAccessorToGenerate accessor)
	{
		var enumRef = accessor.IsNested
			? $"{accessor.ComponentFullName}.{accessor.EnumName}"
			: accessor.EnumName;

		var namesRef = accessor.IsNested
			? $"{accessor.ComponentFullName}.{accessor.NamesClass}"
			: accessor.NamesClass;

		sb.AppendMultiline($$"""
        /// <summary>
        /// Provides extension methods for strongly-typed access to <see cref="{{accessor.FullName}}"/> 
        /// via a <see cref="SlotsMap{T}"/>.
        /// </summary>
        """);

		sb.AppendLine($"public static class {accessor.ExtClassName}");
		sb.AppendLine("{");
		sb.Indent();

		sb.AppendMultiline($$"""
        /// <summary>
        /// Gets the value of the slot identified by the specified <see cref="{{enumRef}}"/> key.
        /// </summary>
        /// <param name="slots">The <see cref="SlotsMap{T}"/> instance containing slot values.</param>
        /// <param name="key">The enum key representing the slot to retrieve.</param>
        /// <returns>The value of the slot, or <c>null</c> if not set.</returns>
        """);

		sb.AppendLine($"public static string? Get{accessor.ComponentTypeParameters}(this {accessor.SlotsMapName} slots, {enumRef} key)");
		sb.Indent();
		sb.AppendLine($" => slots[{namesRef}.NameOf(key)];");
		sb.Dedent();

		foreach (var property in accessor.AllProperties)
		{
			var safe = SymbolHelper.MakeSafeIdentifier(property);
			sb.AppendLine();
			sb.AppendMultiline($$"""
			/// <summary>
			/// Gets the value of the <c>{{property}}</c> slot.
			/// </summary>
			/// <param name="slots">The <see cref="SlotsMap{T}"/> instance containing slot values.</param>
			/// <returns>The value of the <c>{{property}}</c> slot, or <c>null</c> if not set.</returns>
			""");

			sb.AppendLine($"public static string? Get{safe}{accessor.ComponentTypeParameters}(this {accessor.SlotsMapName} slots)");
			sb.Indent();
			sb.AppendLine($" => slots.Get({enumRef}.{safe});");
			sb.Dedent();
		}

		sb.Dedent();
		sb.AppendLine("}");
	}

	private static void WriteISlotsClass(Indenter sb, SlotsAccessorToGenerate accessor)
	{
		sb.AppendLine($"{accessor.Modifiers} {accessor.Name}");
		sb.AppendLine("{");
		sb.Indent();

		string methodModifier;

		if (!accessor.IsGetNameImplemented)
		{
			methodModifier = $"public static{(accessor.IsDirectImplementation ? "" : " new")}";

			// generate the required static mapping method that implements ISlots.GetName
			sb.AppendLine("/// <summary>");
			sb.AppendLine("/// Returns the slot name associated with a property (generated mapping).");
			sb.AppendLine("/// </summary>");
			sb.AppendLine($"{methodModifier} string GetName(string slot)");
			sb.AppendLine("{");
			sb.Indent();
			sb.AppendLine("return slot switch");
			sb.AppendLine("{");
			sb.Indent();

			// accessor.Properties is now tuples (PropertyName, SlotName)
			foreach (var (propName, slotName) in accessor.Properties)
			{
				// map nameof(Property) => "slot-name" (literal)
				sb.AppendLine($"nameof({propName}) => {SymbolHelper.QuoteLiteral(slotName)},");
			}

			sb.AppendLine("_ => slot,"); // fallback to the provided value
			sb.Dedent();
			sb.AppendLine("};");
			sb.Dedent();
			sb.AppendLine("}");
			sb.AppendLine();
		}

		// Determine if methods should be virtual or override
		methodModifier = accessor.IsDirectImplementation
			? (accessor.IsSealed ? "public" : "public virtual")
			: "public override";

		sb.AppendLine($"{methodModifier} IEnumerable<(string Slot, string Value)> EnumerateOverrides()");
		sb.AppendLine("{");
		sb.Indent();

		// If overriding, call base implementation first
		if (!accessor.IsDirectImplementation)
		{
			sb.AppendLine("foreach (var item in base.EnumerateOverrides())");
			sb.Indent();
			sb.AppendLine("yield return item;");
			sb.Dedent();
			sb.AppendLine();
		}

		// Use Properties (own properties only) for EnumerateOverrides
		foreach (var (propName, _) in accessor.Properties)
		{
			sb.AppendLine($"if (!string.IsNullOrWhiteSpace({propName}))");
			sb.Indent();
			sb.AppendLine($"yield return (GetName(nameof({propName})), {propName}!);");
			sb.Dedent();
		}

		sb.Dedent();
		sb.AppendLine("}");

		sb.Dedent();
		sb.AppendLine("}");
	}

	private static void WriteNamesHelper(Indenter sb, SlotsAccessorToGenerate accessor)
	{
		sb.AppendLine($"public static class {accessor.NamesClass}");
		sb.AppendLine("{");
		sb.Indent();

		sb.AppendLine("/// <summary>");
		sb.AppendLine("/// All slot names (read-only).");
		sb.AppendLine("/// </summary>");
        sb.AppendLine($"public static IReadOnlyList<string> AllNames {{ get; }} = Enum.GetNames<{accessor.EnumName}>();");
		sb.AppendLine();
		sb.AppendLine($"/// <summary>Returns the slot name for the given {accessor.EnumName} key.</summary>");
		sb.AppendLine($"public static string NameOf({accessor.EnumName} key) => {accessor.Name}.GetName(key.ToString());");

		sb.Dedent();
		sb.AppendLine("}");

		if (!accessor.IsNested)
		{
			sb.AppendLine();
		}
	}

	private static void WriteNestedClosings(Indenter sb, SlotsAccessorToGenerate accessor)
	{
		ImmutableArray<(string, string)> hierarchy = accessor.Hierarchy;
		for (int i = 0; i < hierarchy.Length - 1; i++)
		{
			sb.Dedent();
			sb.AppendLine("}");
		}

		if (accessor.IsNested)
		{
			sb.AppendLine();
		}
	}

	private static void WriteNestedOpenings(Indenter sb, SlotsAccessorToGenerate accessor)
	{
		ImmutableArray<(string, string)> hierarchy = accessor.Hierarchy;
		foreach (var (name, modifiers) in hierarchy.Take(hierarchy.Length - 1))
		{
			sb.AppendLine($"{modifiers} {name}");
			sb.AppendLine("{");
			sb.Indent();
		}
	}

	private static void WritePragmaClosing(Indenter sb)
	{
		sb.AppendLine();
		sb.AppendLine("#pragma warning restore CS1591");
		sb.AppendLine("#pragma warning restore CS8618");
	}

	private static void WritePreamble(Indenter sb, SlotsAccessorToGenerate accessor)
	{
		sb.AppendLine("// <auto-generated />");
		sb.AppendLine();
		sb.AppendLine("#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member");
		sb.AppendLine("#pragma warning disable CS8618 // Non-nullable field is uninitialized");
		sb.AppendLine();
		sb.AppendLine("using TailwindVariants.NET;");
		sb.AppendLine("using System;");
		sb.AppendLine("using System.Collections.Generic;");
		sb.AppendLine();
		sb.AppendLine("#nullable enable");
		sb.AppendLine();
		if (!string.IsNullOrEmpty(accessor.NamespaceName) && accessor.NamespaceName != "<global namespace>")
		{
			sb.AppendLine($"namespace {accessor.NamespaceName};");
			sb.AppendLine();
		}
	}
}
