namespace TailwindVariants.NET.Docs.Components.Shared;

public partial class Link : ISlotted<Link.Slots>
{
	private static readonly TvDescriptor<Link, Slots> _link = new
	(
		@base: "relative inline-flex items-center outline-solid outline-transparent tap-highlight-transparent cursor-pointer",
		variants: new()
		{
			[l => l.Size] = new Variant<Sizes, Slots>()
			{
				[Sizes.Small] = "text-sm",
				[Sizes.Medium] = "text-base",
				[Sizes.Large] = "text-lg"
			},
			[l => l.Color] = new Variant<Colors, Slots>()
			{
				[Colors.Foreground] = "text-black dark:text-white",
				[Colors.Primary] = "text-sky-600 dark:text-sky-400",
				[Colors.Secondary] = "text-gray-600 dark:text-gray-400",
				[Colors.Success] = "text-green-600 dark:text-green-400",
				[Colors.Warning] = "text-yellow-600 dark:text-yellow-400",
				[Colors.Danger] = "text-red-600 dark:text-red-400",
				[Colors.Info] = "text-teal-600 dark:text-teal-400",
			},
			[l => l.Underline] = new Variant<Underlines, Slots>()
			{
				[Underlines.NoUnderline] = "no-underline",
				[Underlines.Hover] = "hover:underline",
				[Underlines.Always] = "underline",
				[Underlines.Active] = "active:underline",
				[Underlines.Focus] = "focus:underline"
			},
			[l => l.Block] = new Variant<bool, Slots>()
			{
				[true] = new[]
				{
					"px-2",
					"py-1",
					"hover:after:opacity-100",
					"after:content-['']",
					"after:inset-0",
					"after:opacity-0",
					"after:w-full",
					"after:h-full",
					"after:rounded-xl",
					"after:transition-background",
					"after:absolute",
				},
				[false] = "hover:opacity-hover active:opacity-disabled transition-opacity"
			},
			[l => l.Disabled] = new Variant<bool, Slots>()
			{
				[true] = "opacity-disabled cursor-default pointer-events-none",
			}
		}
	);

	public enum Colors
	{
		Foreground,
		Primary,
		Secondary,
		Success,
		Warning,
		Danger,
		Info,
	}

	public enum Sizes
	{
		Small,
		Medium,
		Large
	}

	public enum Underlines
	{
		NoUnderline,
		Hover,
		Always,
		Active,
		Focus
	}

	protected override TvDescriptor<Link, Slots> GetDescriptor() => _link;

	public sealed partial class Slots : ISlots
	{
		public string? Base { get; set; }
	}
}
