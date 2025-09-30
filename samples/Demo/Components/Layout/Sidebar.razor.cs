using TailwindVariants.NET;
using static TailwindVariants.NET.TvFunction;

namespace Demo.Components.Layout;

public partial class Sidebar : ISlotted<Sidebar.Slots>
{
    private static readonly TvReturnType<Sidebar, Slots> _sidebar = Tv<Sidebar, Slots>(new()
    {
        Base = "hidden md:flex md:w-64 md:flex-col md:overflow-auto",
        Slots = new()
        {
            [s => s.Backdrop] = "absolute inset-0 bg-black/40 transition-opacity dark:bg-black/60",
            [s => s.Panel] = "fixed left-0 top-0 h-full w-64 transform transition-transform border-r bg-white shadow-lg dark:bg-neutral-900 dark:border-neutral-800 dark:shadow-none",
            [s => s.Link] = "px-3 py-2 rounded hover:bg-slate-50 dark:hover:bg-slate-700/50",
            [s => s.ActiveLink] = "bg-slate-50! dark:bg-slate-700/50!"
        },
        Variants = new()
        {
            [s => s.Open] = new Variant<bool, Slots>
            {
                [true] = new()
                {
                    [s => s.Backdrop] = "opacity-100",
                    [s => s.Panel] = "translate-x-0",
                },
                [false] = new()
                {
                    [s => s.Backdrop] = "opacity-0 pointer-events-none",
                    [s => s.Panel] = "-translate-x-full",
                }
            }
        }
    });

    // I don't like this one, but OnParametersSet is not being invoked
    private SlotsMap<Slots> Map => _sidebar(this, Tw);

    public sealed class Slots : ISlots
    {
        public string? Backdrop { get; set; }
        public string? Base { get; set; }
        public string? Panel { get; set; }
        public string? Link { get; set; }
        public string? ActiveLink { get; set; }
    }
}

public class SidebarService
{
    public event Action? Changed;

    public bool IsOpen { get; private set; }

    public void Close()
    {
        IsOpen = false;
        NotifyChanged();
    }

    public void NotifyChanged()
    {
        Changed?.Invoke();
    }

    public void Toggle()
    {
        IsOpen = !IsOpen;
        NotifyChanged();
    }
}