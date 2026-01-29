namespace TailwindVariants.NET.Tests;

public class TwVariantsRobustnessTests : BunitContext
{
	public TwVariantsRobustnessTests() => Services.AddTailwindVariants();

	private TwVariants Tv => Services.GetRequiredService<TwVariants>();

	[Fact]
	public void Inheritance_Order_IsPreserved_ForBaseSlot()
	{
		// parent has p-1 then child adds text-sm; ancestor token should appear before child token
		var parent = new TvDescriptor<ButtonComponent, ButtonSlots>(@base: ["p-1", "bg-red-500"]);
		var child = new TvDescriptor<GhostButtonComponent, GhostButtonSlots>(extends: parent, @base: ["text-sm", "bg-blue-500"]);

		var result = Tv.Invoke(new GhostButtonComponent(), child);
		var tokens = result[s => s.Base]?.Split(' ', StringSplitOptions.RemoveEmptyEntries);

		Assert.NotNull(tokens);
		var idxAncestor = Array.IndexOf(tokens, "p-1");
		var idxChild = Array.IndexOf(tokens, "text-sm");

		Assert.True(idxAncestor >= 0 && idxChild >= 0, "expected tokens present");
		Assert.True(idxAncestor < idxChild, $"expected ancestor token before child token; got {string.Join(' ', tokens)}");
	}

	[Fact]
	public void Invoke_DoesNotThrow_When_CompoundPredicateThrows()
	{
		var descriptor = new TvDescriptor<ComponentWithThrowingPredicate, TestSlots>(
			@base: "btn",
			compoundVariants:
			[
				new(c => c.PredicateThrows()) { Class = "x" }
			]
		);

		var instance = new ComponentWithThrowingPredicate();

		var ex = Record.Exception(() => Tv.Invoke(instance, descriptor));
		Assert.Null(ex);
	}

	[Fact]
	public void Invoke_DoesNotThrow_When_VariantAccessorThrows()
	{
		// Test component whose accessor throws in getter.
		var descriptor = new TvDescriptor<ComponentWithThrowingAccessor, TestSlots>(
			@base: "btn",
			variants: new VariantCollection<ComponentWithThrowingAccessor, TestSlots>
			{
				{
					c => c.Size, // getter will throw during evaluation
					new Variant<string, TestSlots>
					{
						["x"] = "text-x"
					}
				}
			}
		);

		var broken = new ComponentWithThrowingAccessor();

		// Should NOT throw (compiled variant's Apply swallows exceptions).
		var ex = Record.Exception(() => Tv.Invoke(broken, descriptor));
		Assert.Null(ex);
	}
}
