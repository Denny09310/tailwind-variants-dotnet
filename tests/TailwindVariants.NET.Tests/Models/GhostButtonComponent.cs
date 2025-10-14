namespace TailwindVariants.NET.Tests;

public partial class GhostButtonComponent : ButtonComponent, ISlotted<GhostButtonSlots>
{
	public new GhostButtonSlots? Classes { get; set; }
	public bool IsSquared { get; set; }
}
