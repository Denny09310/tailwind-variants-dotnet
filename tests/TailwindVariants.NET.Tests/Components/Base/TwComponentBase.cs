using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace TailwindVariants.NET.Tests.Components;

public abstract class TwComponentBase<TOwner, TSlots> : ComponentBase
    where TSlots : ISlots, new()
    where TOwner : ISlotted<TSlots>
{
    protected SlotsMap<TSlots> _slots = new();

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Inject]
    protected TwVariants Tv { get; set; } = default!;

    protected void ComputeStyles()
    {
        if (this is TOwner owner)
        {
            _slots = Tv.Invoke(owner, GetDescriptor());
        }
    }

    protected abstract TvDescriptor<TOwner, TSlots> GetDescriptor();

    protected override void OnParametersSet()
    {
        ComputeStyles();

        base.OnParametersSet();
    }
}