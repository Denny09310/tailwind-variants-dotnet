using System.Collections.Generic;

using Microsoft.AspNetCore.Components;

namespace TailwindVariants.NET;

/// <summary>
/// Base component that integrates TailwindMerge and allows passing through additional HTML attributes.
/// </summary>
public abstract class TwComponentBase<TOwner, TSlots> : ComponentBase
	where TSlots : ISlots, new()
	where TOwner : ISlotted<TSlots>
{
	protected SlotsMap<TSlots> _slots = new();

	/// <summary>
	/// Additional HTML attributes that will be splatted onto the root element.
	/// </summary>
	[Parameter(CaptureUnmatchedValues = true)]
	public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

	/// <summary>
	/// Optional explicit class string available on components.
	/// </summary>
	[Parameter]
	public string? Class { get; set; }

	/// <summary>
	/// TailwindMerge service used to merge class strings in a conflict-aware manner.
	/// </summary>
	[Inject]
	protected TwVariants Tv { get; set; } = default!;

	protected abstract TvDescriptor<TOwner, TSlots> GetDescriptor();

	protected override void OnParametersSet()
	{
		if (this is TOwner owner)
		{
			_slots = Tv.Invoke(owner, GetDescriptor());
		}

		base.OnParametersSet();
	}
}
