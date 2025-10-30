namespace TailwindVariants.NET.Tests.Models;

public partial class GenericComponent<T> : ISlottable<GenericComponent<T>.Slots>
{
	public string? Class { get; set; }

	public Slots? Classes { get; set; }

	public sealed partial class Slots : ISlots
	{
		public string? Base { get; set; }
	}
}
