namespace TailwindVariants.NET.Tests;

#pragma warning disable CS0436

public partial class TestSlots : ISlots
{
    public string? Base { get; set; }

    public string? Container { get; set; }

    [Slot("descr")]
    public string? Description { get; set; }

    public string? Title { get; set; }
}

#pragma warning restore CS0436