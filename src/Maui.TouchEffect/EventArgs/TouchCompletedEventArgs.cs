﻿namespace Maui.TouchEffect;

public class TouchCompletedEventArgs : EventArgs
{
	internal TouchCompletedEventArgs(object? parameter)
	{
		Parameter = parameter;
	}

	public object? Parameter { get; }
}
