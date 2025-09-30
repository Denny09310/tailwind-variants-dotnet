using Microsoft.AspNetCore.Components;
using TailwindMerge;

namespace TailwindVariants.NET;

/// <summary>
/// Base component that integrates TailwindMerge and allows passing through additional HTML attributes.
/// </summary>
public partial class TailwindComponentBase : ComponentBase
{
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
    protected TwMerge Tw { get; set; } = default!;
}
