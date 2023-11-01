using CoreGraphics;
using Foundation;
using Maui.TouchEffect.Enums;
using MauiTouchEffect.Extensions;
using Microsoft.Maui.Controls.Compatibility.Platform.iOS;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Platform;
using UIKit;
namespace Maui.TouchEffect;

public partial class PlatformTouchEffect : PlatformEffect
{
    private UIGestureRecognizer? touchGesture;
    private UIGestureRecognizer? hoverGesture;
    
    TouchEffect? effect;
    UIView View => Container ?? Control;
    
    protected override void OnAttached()
    {
        effect = TouchEffect.PickFrom(Element);
        if (effect?.IsDisabled ?? true)
            return;

        effect.Element = (VisualElement)Element;

        if (View == null)
            return;

        touchGesture = new TouchUITapGestureRecognizer(effect);

        if (((View as IVisualNativeElementRenderer)?.Control ?? View) is UIButton button)
        {
            button.AllTouchEvents += PreventButtonHighlight;
            ((TouchUITapGestureRecognizer)touchGesture).IsButton = true;
        }

        View.AddGestureRecognizer(touchGesture);

        if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
        {
            hoverGesture = new UIHoverGestureRecognizer(OnHover);
            View.AddGestureRecognizer(hoverGesture);
        }

        View.UserInteractionEnabled = true;
    }

    protected override void OnDetached()
    {
        throw new NotImplementedException();
    }
    
    
    private void OnHover()
    {
        if (effect == null || effect.IsDisabled)
            return;

        switch (hoverGesture?.State)
        {
            case UIGestureRecognizerState.Began:
            case UIGestureRecognizerState.Changed:
                effect.HandleHover(HoverStatus.Entered);
                break;
            case UIGestureRecognizerState.Ended:
                effect.HandleHover(HoverStatus.Exited);
                break;
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
}

internal sealed class TouchUITapGestureRecognizer : UIGestureRecognizer
{
	private TouchEffect touchEffect;
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
		if (touchEffect?.IsDisabled ?? true)
		{
			return;
		}

		IsCanceled = false;
		startPoint = GetTouchPoint(touches);

		HandleTouch(TouchStatus.Started, TouchInteractionStatus.Started).SafeFireAndForget();

		base.TouchesBegan(touches, evt);
	}

	public override void TouchesEnded(NSSet touches, UIEvent evt)
	{
		if (touchEffect?.IsDisabled ?? true)
		{
			return;
		}

		HandleTouch(touchEffect?.Status == TouchStatus.Started ? TouchStatus.Completed : TouchStatus.Canceled, TouchInteractionStatus.Completed).SafeFireAndForget();

		IsCanceled = true;

		base.TouchesEnded(touches, evt);
	}

	public override void TouchesCancelled(NSSet touches, UIEvent evt)
	{
		if (touchEffect?.IsDisabled ?? true)
		{
			return;
		}

		HandleTouch(TouchStatus.Canceled, TouchInteractionStatus.Completed).SafeFireAndForget();

		IsCanceled = true;

		base.TouchesCancelled(touches, evt);
	}

	public override void TouchesMoved(NSSet touches, UIEvent evt)
	{
		if (touchEffect?.IsDisabled ?? true)
		{
			return;
		}

		var disallowTouchThreshold = touchEffect.DisallowTouchThreshold;
		var point = GetTouchPoint(touches);
		if (point != null && startPoint != null && disallowTouchThreshold > 0)
		{
			var diffX = Math.Abs(point.Value.X - startPoint.Value.X);
			var diffY = Math.Abs(point.Value.Y - startPoint.Value.Y);
			var maxDiff = Math.Max(diffX, diffY);
			if (maxDiff > disallowTouchThreshold)
			{
				HandleTouch(TouchStatus.Canceled, TouchInteractionStatus.Completed).SafeFireAndForget();
				IsCanceled = true;
				base.TouchesMoved(touches, evt);
				return;
			}
		}

		var status = point != null && View?.Bounds.Contains(point.Value) is true
			? TouchStatus.Started
			: TouchStatus.Canceled;

		if (touchEffect?.Status != status)
		{
			HandleTouch(status).SafeFireAndForget();
		}

		if (status == TouchStatus.Canceled)
		{
			IsCanceled = true;
		}

		base.TouchesMoved(touches, evt);
	}

	public async Task HandleTouch(TouchStatus status, TouchInteractionStatus? interactionStatus = null)
	{
		if (IsCanceled || touchEffect == null)
		{
			return;
		}

		if (touchEffect?.IsDisabled ?? true)
		{
			return;
		}

		var canExecuteAction = touchEffect.CanExecute;

		if (interactionStatus == TouchInteractionStatus.Started)
		{
			touchEffect?.HandleUserInteraction(TouchInteractionStatus.Started);
			interactionStatus = null;
		}

		touchEffect?.HandleTouch(status);
		if (interactionStatus.HasValue)
		{
			touchEffect?.HandleUserInteraction(interactionStatus.Value);
		}

		if (touchEffect == null || touchEffect.Element is null || (!touchEffect.NativeAnimation && !IsButton) || (!canExecuteAction && status == TouchStatus.Started))
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
		return (touches?.AnyObject as UITouch)?.LocationInView(View);
	}

	private class TouchUITapGestureRecognizerDelegate : UIGestureRecognizerDelegate
	{
		public override bool ShouldRecognizeSimultaneously(UIGestureRecognizer gestureRecognizer, UIGestureRecognizer otherGestureRecognizer)
		{
			if (gestureRecognizer is TouchUITapGestureRecognizer touchGesture && otherGestureRecognizer is UIPanGestureRecognizer &&
			    otherGestureRecognizer.State == UIGestureRecognizerState.Began)
			{
				touchGesture.HandleTouch(TouchStatus.Canceled, TouchInteractionStatus.Completed).SafeFireAndForget();
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

			return recognizer.View.Subviews.Any(view => view == touch.View);
		}
	}
}