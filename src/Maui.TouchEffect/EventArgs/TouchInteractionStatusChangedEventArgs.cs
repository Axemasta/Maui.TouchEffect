﻿using Maui.TouchEffect.Enums;
namespace Maui.TouchEffect;

public class TouchInteractionStatusChangedEventArgs : EventArgs
{
	internal TouchInteractionStatusChangedEventArgs(TouchInteractionStatus touchInteractionStatus)
	{
		TouchInteractionStatus = touchInteractionStatus;
	}

	public TouchInteractionStatus TouchInteractionStatus { get; }
}
