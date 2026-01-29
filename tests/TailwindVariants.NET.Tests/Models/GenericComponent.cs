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

public partial class GenericComponent<T1, T2> : ISlottable<GenericComponent<T1, T2>.Slots>
	where T1 : notnull
	where T2 : class
{
	public string? Class { get; set; }

	public Slots? Classes { get; set; }

	public sealed partial class Slots : ISlots
	{
		public string? Base { get; set; }
	}
}
