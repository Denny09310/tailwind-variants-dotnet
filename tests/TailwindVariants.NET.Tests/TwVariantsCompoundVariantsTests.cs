using TailwindVariants.NET.Core;
using TailwindVariants.NET.Tests.Models;

namespace TailwindVariants.NET.Tests;

public sealed class TwVariantsCompoundVariantsTests
{
	[Fact]
	public void CompoundVariant_Applies_WhenAllConditionsMatch()
	{
		var tv = TwVariants<TestProps>.Create(
			variants: new()
			{
				["Size"] = new()
				{
					["lg"] = "text-lg"
				},
				["Color"] = new()
				{
					["red"] = "text-red-500"
				}
			},
			compoundVariants:
			[
				new(
					new Dictionary<string, string>
					{
						["Size"] = "lg",
						["Color"] = "red"
					},
					"font-bold"
				)
			]
		);

		var result = tv.Invoke(new TestProps
		{
			Size = "lg",
			Color = "red"
		});

		Assert.Equal("text-lg text-red-500 font-bold", result);
	}

	[Fact]
	public void CompoundVariant_IsSkipped_WhenAnyConditionFails()
	{
		var tv = TwVariants<TestProps>.Create(
			compoundVariants:
			[
				new(
					new Dictionary<string, string>
					{
						["Size"] = "lg",
						["Color"] = "red"
					},
					"font-bold"
				)
			]
		);

		var result = tv.Invoke(new TestProps
		{
			Size = "lg",
			Color = "blue"
		});

		Assert.Equal(string.Empty, result);
	}
}
