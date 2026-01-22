using TailwindVariants.NET.Core;
using TailwindVariants.NET.Tests.Models;

namespace TailwindVariants.NET.Tests;

public sealed class TwVariantsVariantsTests
{
	[Fact]
	public void Variant_IsApplied_WhenValueMatches()
	{
		var tv = TwVariants<TestProps>.Create(
			variants: new()
			{
				["Size"] = new()
				{
					["sm"] = "text-sm",
					["lg"] = "text-lg"
				}
			}
		);

		var result = tv.Invoke(new TestProps { Size = "lg" });

		Assert.Equal("text-lg", result);
	}

	[Fact]
	public void UnknownVariantValue_IsIgnored()
	{
		var tv = TwVariants<TestProps>.Create(
			variants: new()
			{
				["Size"] = new()
				{
					["sm"] = "text-sm"
				}
			}
		);

		var result = tv.Invoke(new TestProps { Size = "xl" });

		Assert.Equal(string.Empty, result);
	}

	[Fact]
	public void VariantName_IsCaseInsensitive()
	{
		var tv = TwVariants<TestProps>.Create(
			variants: new()
			{
				["size"] = new()
				{
					["sm"] = "text-sm"
				}
			}
		);

		var result = tv.Invoke(new TestProps { Size = "sm" });

		Assert.Equal("text-sm", result);
	}
}
