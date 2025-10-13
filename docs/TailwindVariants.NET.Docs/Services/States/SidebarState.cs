namespace TailwindVariants.NET.Docs.Services;

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
