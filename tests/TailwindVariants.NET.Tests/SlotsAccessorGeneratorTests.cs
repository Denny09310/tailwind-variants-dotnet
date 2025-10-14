using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace TailwindVariants.NET.SourceGenerators.Tests;

public class SlotsAccessorGeneratorTests
{
	private const string CommonRuntimeTypes = """
        namespace TailwindVariants.NET
        {
            public interface ISlots { string? Base { get; } }
            public class SlotsMap<T>
            {
                public System.Collections.Generic.Dictionary<string, string?> Map { get; } = new();
            }
        }
        """;

	[Fact]
	public void Generates_EnumerateOverrides_Enum_Names_And_Extensions_For_Simple_Slots()
	{
		var input = """
            namespace Demo.Components
            {
                public partial class MyComponent
                {
                    public partial class Slots : TailwindVariants.NET.ISlots
                    {
                        public string? Base { get; set; }
                        public string? Header { get; set; }
                    }
                }
            }
            """;

		var (generated, diags) = RunGenerator(input);

		Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
		Assert.NotEmpty(generated);

		var combined = string.Join("\n---GEN---\n", generated.Select(gs => gs.SourceText.ToString()));

		Assert.Contains("EnumerateOverrides()", combined);
		Assert.Contains("public enum SlotsTypes", combined);
		Assert.Contains("public static class SlotsNames", combined);
		Assert.Contains("public static string? GetHeader", combined);
	}

	[Fact]
	public void Handles_Nested_Types_Correctly()
	{
		var input = """
            namespace Demo.NestedSample
            {
                public partial class Outer
                {
                    public partial class Inner
                    {
                        public partial class Slots : TailwindVariants.NET.ISlots
                        {
                            public string? Base { get; set; }
                            public string? Footer { get; set; }
                        }
                    }
                }
            }
            """;

		var (generated, diags) = RunGenerator(input);

		Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
		Assert.NotEmpty(generated);

		var combined = string.Join("\n---GEN---\n", generated.Select(gs => gs.SourceText.ToString()));
		Assert.Contains("partial class Outer", combined);
		Assert.Contains("partial class Inner", combined);
		Assert.Contains("GetFooter", combined);
	}

	[Fact]
	public void Ignores_NonString_Properties()
	{
		var input = """
            namespace Demo.Components
            {
                public partial class MyComponent
                {
                    public partial class Slots : TailwindVariants.NET.ISlots
                    {
                        public string? Base { get; set; }
                        public int Count { get; set; }
                    }
                }
            }
            """;

		var (generated, diags) = RunGenerator(input);

		Assert.NotEmpty(generated);
		var combined = string.Join("\n---GEN---\n", generated.Select(gs => gs.SourceText.ToString()));

		Assert.Contains("GetBase", combined);
		Assert.DoesNotContain("GetCount", combined);
	}

	[Fact]
	public void ShouldNot_Generate_When_No_Public_Properties()
	{
		var input = """
            namespace Demo.Components
            {
                public partial class MyComponent
                {
                    public partial class Slots : TailwindVariants.NET.ISlots
                    {
                    }
                }
            }
            """;

		var (generated, _) = RunGenerator(input);

		Assert.Empty(generated);
	}

	[Fact]
	public void ShouldNot_Generate_When_Slots_Not_Partial()
	{
		var input = """
            namespace Demo.Components
            {
                public class MyComponent
                {
                    public class Slots : TailwindVariants.NET.ISlots
                    {
                        public string? Base { get; set; }
                    }
                }
            }
            """;

		var (generated, _) = RunGenerator(input);

		Assert.Empty(generated);
	}

	private static (ImmutableArray<GeneratedSourceResult> Generated, ImmutableArray<Diagnostic> Diagnostics) RunGenerator(params string[] sources)
	{
		var allSources = new List<SyntaxTree>
		{
			CSharpSyntaxTree.ParseText(SourceText.From(CommonRuntimeTypes, Encoding.UTF8),
				new CSharpParseOptions(LanguageVersion.Latest, kind: SourceCodeKind.Regular))
		};

		allSources.AddRange(sources.Select(s => CSharpSyntaxTree.ParseText(SourceText.From(s, Encoding.UTF8),
			new CSharpParseOptions(LanguageVersion.Latest, kind: SourceCodeKind.Regular))));

		var refs = AppDomain.CurrentDomain.GetAssemblies()
			.Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
			.Select(a => MetadataReference.CreateFromFile(a.Location))
			.Cast<MetadataReference>()
			.ToList();

		var compilation = CSharpCompilation.Create(
			assemblyName: "GeneratorTests_" + Guid.NewGuid().ToString("N"),
			syntaxTrees: allSources,
			references: refs,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var generator = new SlotsAccessorGenerator();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

		var runResult = driver.GetRunResult();
		var result = runResult.Results.Single();
		return (result.GeneratedSources, result.Diagnostics);
	}
}
