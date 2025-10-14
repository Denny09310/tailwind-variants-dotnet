namespace TailwindVariants.NET.Tests;

public partial class ButtonSlots : ISlots
{
	[Slot("base")]
	public string? Base { get; set; }

	public string? Icon { get; set; }

	public string? Label { get; set; }
}
