using TailwindVariants.NET.Core;
using TailwindVariants.NET.Tests.Models;

namespace TailwindVariants.NET.Tests;

public sealed class TwVariantsNullHandlingTests
{
	[Fact]
	public void MissingProps_DoNotThrow()
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

		var result = tv.Invoke(new TestProps());

		Assert.Equal(string.Empty, result);
	}
}
