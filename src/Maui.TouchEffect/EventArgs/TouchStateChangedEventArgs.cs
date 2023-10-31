using Maui.TouchEffect.Enums;
namespace Maui.TouchEffect;

public class TouchStateChangedEventArgs : EventArgs
{
	internal TouchStateChangedEventArgs(TouchState state)
	{
		State = state;
	}

	public TouchState State { get; }
}
