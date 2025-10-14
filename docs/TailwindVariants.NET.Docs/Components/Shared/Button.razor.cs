namespace TailwindVariants.NET.Docs.Components.Shared;

public partial class Button : ISlotted<Button.Slots>
{
	public static readonly TvDescriptor<Button, Slots> _button = new();

	public enum Variants
	{
		Primary,
		Secondary,
		Danger
	}

	protected override TvDescriptor<Button, Slots> GetDescriptor() => _button;

	public sealed partial class Slots : ISlots
	{
		public string? Base { get; set; }
		public string? Icon { get; set; }
	}
}
