namespace TailwindVariants.NET.Docs.Components.Docs.Layout
{
    public partial class Sidebar : ISlotted<Sidebar.Slots>
    {
        private static readonly TvDescriptor<Sidebar, Slots> _sidebar = new
        (
            @base: "fixed top-0 left-0 h-full w-64 z-50 transform transition-transform duration-300 bg-white dark:bg-neutral-900 md:h-auto md:sticky md:top-16 md:block md:self-start md:bg-transparent!",
            slots: new()
            {
                [s => s.Overlay] = "transition-[opacity]",
                [s => s.Wrapper] = "p-4 flex flex-col gap-1 overflow-y-auto",
                [s => s.Header] = "flex items-center justify-between p-4 border-b border-neutral-200 dark:border-neutral-700 md:hidden",
                [s => s.Link] = "rounded px-3 py-2 text-sm hover:bg-sky-200/50 dark:hover:bg-sky-700/50",
                [s => s.LinkHeader] = "mt-3 font-semibold text-neutral-600 dark:text-neutral-300",
                [s => s.LinkActive] = "bg-sky-200/50! dark:bg-sky-700/50!",
            },
            variants: new()
            {
                [s => s.Open] = new Variant<bool, Slots>
                {
                    [true] = new()
                    {
                        [s => s.Base] = "translate-x-0",
                        [s => s.Overlay] = "fixed inset-0 bg-black opacity-50 z-40",
                    },
                    [false] = new()
                    {
                        [s => s.Base] = "-translate-x-full md:translate-x-0",
                        [s => s.Overlay] = "opacity-0 pointer-events-none"
                    }
                }
            }
        );

        protected override TvDescriptor<Sidebar, Slots> GetDescriptor() => _sidebar;

        public sealed partial class Slots : ISlots
        {
            public string? Base { get; set; }
            public string? Header { get; set; }
            public string? Link { get; set; }
            public string? LinkActive { get; set; }
            public string? LinkHeader { get; set; }
            public string? Overlay { get; set; }
            public string? Wrapper { get; set; }
        }
    }
}

namespace TailwindVariants.NET.Docs.Services
{
    public class SidebarState
    {
        public event Action? OnChange;

        public bool IsOpen { get; set; }

        public void Close()
        {
            IsOpen = false;
            NotifyStateChanged();
        }

        public void Toggle()
        {
            IsOpen = !IsOpen;
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}