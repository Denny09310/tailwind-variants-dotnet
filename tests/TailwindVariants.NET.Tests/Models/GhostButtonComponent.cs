namespace TailwindVariants.NET.Tests;

public partial class GhostButtonComponent : ButtonComponent, ISlottable<GhostButtonSlots>
{
	public new GhostButtonSlots? Classes { get; set; }
	public bool IsSquared { get; set; }
}
