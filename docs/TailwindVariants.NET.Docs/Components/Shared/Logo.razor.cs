namespace TailwindVariants.NET.Docs.Components.Shared;

public partial class Logo : ISlotted<Logo.Slots>
{
	private static readonly TvDescriptor<Logo, Slots> _logo = new();

	protected override TvDescriptor<Logo, Slots> GetDescriptor() => _logo;

	public sealed partial class Slots : ISlots
	{
		public string? Base { get; set; }
	}
}
