namespace Maui.TouchEffect;

public class LongPressCompletedEventArgs : EventArgs
{
	internal LongPressCompletedEventArgs(object? parameter)
	{
		Parameter = parameter;
	}

	public object? Parameter { get; }
}
