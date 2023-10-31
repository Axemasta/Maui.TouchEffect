using Maui.TouchEffect.Enums;
namespace Maui.TouchEffect;

public class HoverStatusChangedEventArgs : EventArgs
{
	internal HoverStatusChangedEventArgs(HoverStatus status)
	{
		Status = status;
	}

	public HoverStatus Status { get; }
}
