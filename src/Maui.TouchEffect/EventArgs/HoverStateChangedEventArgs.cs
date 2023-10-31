using Maui.TouchEffect.Enums;
namespace Maui.TouchEffect;

public class HoverStateChangedEventArgs : EventArgs
{
	internal HoverStateChangedEventArgs(HoverState state)
	{
		State = state;
	}

	public HoverState State { get; }
}
