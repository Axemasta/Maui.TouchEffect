using CoreGraphics;
using Foundation;
using Maui.TouchEffect.Enums;
using MauiTouchEffect.Extensions;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Platform;
using UIKit;
namespace Maui.TouchEffect;

public partial class PlatformTouchEffect : PlatformEffect
{
    private UIGestureRecognizer? touchGesture;
    private UIGestureRecognizer? hoverGesture;
    
    TouchEffect? touchEffect;
    UIView View => Container ?? Control;
    
    protected override void OnAttached()
    {
        touchEffect = TouchEffect.PickFrom(Element);

        if (touchEffect?.IsDisabled ?? true)
        {
            return;
        }

        touchEffect.Element = (VisualElement)Element;
        
        touchGesture = new TouchUITapGestureRecognizer(touchEffect);

        if (Control is UIButton button)
        {
            button.AllTouchEvents += PreventButtonHighlight;
            ((TouchUITapGestureRecognizer)touchGesture).IsButton = true;
            button.AddGestureRecognizer(touchGesture);
        }
        else
        {
            View.AddGestureRecognizer(touchGesture);
        }

        if (OperatingSystem.IsIOSVersionAtLeast(13))
        {
            hoverGesture = new UIHoverGestureRecognizer(OnHover);
            View.AddGestureRecognizer(hoverGesture);
        }

        View.UserInteractionEnabled = true;
    }

    protected override void OnDetached()
    {
        if (Control is UIButton button)
        {
            button.AllTouchEvents -= PreventButtonHighlight;
        }
        
        if (touchGesture is not null)
        {
            View.RemoveGestureRecognizer(touchGesture);
            touchGesture?.Dispose();
            touchGesture = null;
        }
        
        if (hoverGesture is not null)
        {
            View.RemoveGestureRecognizer(hoverGesture);
            hoverGesture?.Dispose();
            hoverGesture = null;
        }
        
        if (touchEffect is not null)
        {
            touchEffect.Element = null;
        }
    }
    
    private void PreventButtonHighlight(object? sender, EventArgs args)
    {
        if (sender is not UIButton button)
        {
            throw new ArgumentException($"{nameof(sender)} must be Type {nameof(UIButton)}", nameof(sender));
        }

        button.Highlighted = false;
    }
    
    private void OnHover()
    {
        if (touchEffect?.IsDisabled ?? true)
        {
            return;
        }

        switch (hoverGesture?.State)
        {
            case UIGestureRecognizerState.Began:
            case UIGestureRecognizerState.Changed:
                touchEffect.HandleHover(HoverStatus.Entered);
                break;
            case UIGestureRecognizerState.Ended:
                touchEffect.HandleHover(HoverStatus.Exited);
                break;
        }
    }
}

// ReSharper disable once InconsistentNaming
internal sealed class TouchUITapGestureRecognizer : UIGestureRecognizer
{
	private readonly TouchEffect touchEffect;
	private float? defaultRadius;
	private float? defaultShadowRadius;
	private float? defaultShadowOpacity;
	private CGPoint? startPoint;

	public TouchUITapGestureRecognizer(TouchEffect touchEffect)
	{
		this.touchEffect = touchEffect;
		CancelsTouchesInView = false;
		Delegate = new TouchUITapGestureRecognizerDelegate();
	}

	public bool IsCanceled { get; set; } = true;

	public bool IsButton { get; set; }

	public override void TouchesBegan(NSSet touches, UIEvent evt)
	{
        base.TouchesBegan(touches, evt);
        
		if (touchEffect.IsDisabled)
		{
			return;
		}

		IsCanceled = false;
		startPoint = GetTouchPoint(touches);

		HandleTouch(TouchStatus.Started, TouchInteractionStatus.Started).SafeFireAndForget();
	}

	public override void TouchesEnded(NSSet touches, UIEvent evt)
	{
        base.TouchesEnded(touches, evt);
        
		if (touchEffect.IsDisabled)
		{
			return;
		}

		HandleTouch(touchEffect.CurrentTouchStatus == TouchStatus.Started ? TouchStatus.Completed : TouchStatus.Cancelled, TouchInteractionStatus.Completed).SafeFireAndForget();

		IsCanceled = true;
	}

	public override void TouchesCancelled(NSSet touches, UIEvent evt)
	{
        base.TouchesCancelled(touches, evt);
        
		if (touchEffect.IsDisabled)
		{
			return;
		}

		HandleTouch(TouchStatus.Cancelled, TouchInteractionStatus.Completed).SafeFireAndForget();

		IsCanceled = true;
	}

	public override void TouchesMoved(NSSet touches, UIEvent evt)
	{
        base.TouchesMoved(touches, evt);
        
		if (touchEffect.IsDisabled)
		{
			return;
		}

		var disallowTouchThreshold = touchEffect.DisallowTouchThreshold;
		var point = GetTouchPoint(touches);
		if (point is not null && startPoint is not null && disallowTouchThreshold > 0)
		{
			var diffX = Math.Abs(point.Value.X - startPoint.Value.X);
			var diffY = Math.Abs(point.Value.Y - startPoint.Value.Y);
			var maxDiff = Math.Max(diffX, diffY);
			if (maxDiff > disallowTouchThreshold)
			{
				HandleTouch(TouchStatus.Cancelled, TouchInteractionStatus.Completed).SafeFireAndForget();
				IsCanceled = true;
				return;
			}
		}

		var status = point != null && View?.Bounds.Contains(point.Value) is true
			? TouchStatus.Started
			: TouchStatus.Cancelled;

		if (touchEffect?.CurrentTouchStatus != status)
		{
			HandleTouch(status).SafeFireAndForget();
		}

		if (status == TouchStatus.Cancelled)
		{
			IsCanceled = true;
		}
	}

	public async Task HandleTouch(TouchStatus status, TouchInteractionStatus? interactionStatus = null)
	{
		if (IsCanceled)
		{
			return;
		}

		if (touchEffect.IsDisabled)
		{
			return;
		}

		var canExecuteAction = touchEffect.CanExecute;

		if (interactionStatus == TouchInteractionStatus.Started)
		{
			touchEffect.HandleUserInteraction(TouchInteractionStatus.Started);
			interactionStatus = null;
		}

		touchEffect.HandleTouch(status);
		if (interactionStatus.HasValue)
		{
			touchEffect.HandleUserInteraction(interactionStatus.Value);
		}
        
        if (touchEffect.Element is null
            || (!touchEffect.NativeAnimation && !IsButton)
            || (!canExecuteAction && status is TouchStatus.Started))
        {
            return;
        }

		var color = touchEffect.NativeAnimationColor;
		var radius = touchEffect.NativeAnimationRadius;
		var shadowRadius = touchEffect.NativeAnimationShadowRadius;
		var isStarted = status == TouchStatus.Started;
        
		defaultRadius = (float?) (defaultRadius ?? View.Layer.CornerRadius);
		defaultShadowRadius = (float?) (defaultShadowRadius ?? View.Layer.ShadowRadius);
		defaultShadowOpacity ??= View.Layer.ShadowOpacity;

		var tcs = new TaskCompletionSource<UIViewAnimatingPosition>();
		UIViewPropertyAnimator.CreateRunningPropertyAnimator(.2, 0, UIViewAnimationOptions.AllowUserInteraction,
			() =>
			{
				if (color is null)
				{
					View.Layer.Opacity = isStarted ? 0.5f : (float) touchEffect.Element.Opacity;
				}
				else
                {
                    var backgroundColor = touchEffect.Element.BackgroundColor ?? Colors.Transparent;
                    
					View.Layer.BackgroundColor = (isStarted ? color : backgroundColor).ToCGColor();
				}

				View.Layer.CornerRadius = isStarted ? radius : defaultRadius.GetValueOrDefault();

				if (shadowRadius >= 0)
				{
					View.Layer.ShadowRadius = isStarted ? shadowRadius : defaultShadowRadius.GetValueOrDefault();
					View.Layer.ShadowOpacity = isStarted ? 0.7f : defaultShadowOpacity.GetValueOrDefault();
				}
			}, endPos => tcs.SetResult(endPos));
		await tcs.Task;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			Delegate.Dispose();
		}

		base.Dispose(disposing);
	}

	private CGPoint? GetTouchPoint(NSSet touches)
	{
		return (touches.AnyObject as UITouch)?.LocationInView(View);
	}

	private class TouchUITapGestureRecognizerDelegate : UIGestureRecognizerDelegate
	{
		public override bool ShouldRecognizeSimultaneously(UIGestureRecognizer gestureRecognizer, UIGestureRecognizer otherGestureRecognizer)
		{
			if (gestureRecognizer is TouchUITapGestureRecognizer touchGesture && otherGestureRecognizer is UIPanGestureRecognizer &&
			    otherGestureRecognizer.State == UIGestureRecognizerState.Began)
			{
				touchGesture.HandleTouch(TouchStatus.Cancelled, TouchInteractionStatus.Completed).SafeFireAndForget();
				touchGesture.IsCanceled = true;
			}

			return true;
		}

		public override bool ShouldReceiveTouch(UIGestureRecognizer recognizer, UITouch touch)
		{
			if (recognizer.View.IsDescendantOfView(touch.View))
			{
				return true;
			}

            return touch.View.IsDescendantOfView(recognizer.View) && (touch.View.GestureRecognizers is null || touch.View.GestureRecognizers.Length == 0);
		}
	}
}