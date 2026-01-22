using TailwindVariants.NET.Core;
using TailwindVariants.NET.Tests.Models;

namespace TailwindVariants.NET.Tests;

public sealed class TwVariantsBasicTests
{
	[Fact]
	public void BaseClasses_AreAlwaysApplied()
	{
		var tv = TwVariants<TestProps>.Create(
			@base: "inline-flex items-center"
		);

		var result = tv.Invoke(new TestProps());

		Assert.Equal("inline-flex items-center", result);
	}
}
