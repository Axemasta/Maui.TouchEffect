using System.ComponentModel;

using Android.Content;
using Android.Content.Res;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Views.Accessibility;
using Android.Widget;

using Maui.TouchEffect.Enums;

using Microsoft.Maui.Platform;

using AView = Android.Views.View;
using Color = Android.Graphics.Color;
using MColor = Microsoft.Maui.Graphics.Color;
using MView = Microsoft.Maui.Controls.View;

namespace Maui.TouchEffect;

public class PlatformTouchEffect : Microsoft.Maui.Controls.Platform.PlatformEffect
{
    private static readonly MColor defaultNativeAnimationColor = new(128, 128, 128, 64);

    AccessibilityManager? accessibilityManager;
    AccessibilityListener? accessibilityListener;

    TouchEffect? touchEffect;

    bool isHoverSupported;
    float startX;
    float startY;

    RippleDrawable? ripple;
    AView? rippleView;
    MColor? rippleColor;
    int rippleRadius = -1;

    AView View => Control ?? Container;

    ViewGroup? ViewGroup => (Container ?? Control) as ViewGroup;

    bool IsAccessibilityMode => accessibilityManager is not null
        && accessibilityManager.IsEnabled
        && accessibilityManager.IsTouchExplorationEnabled;

    private readonly bool isAtLeastM = OperatingSystem.IsAndroidVersionAtLeast((int)BuildVersionCodes.M);

    internal bool IsCanceled { get; set; }

    bool IsForegroundRippleWithTapGestureRecognizer => 
        ripple != null &&
        ripple.IsAlive() &&
        View.IsAlive() &&
        View.Foreground == ripple &&
        Element is MView mView &&
        mView.GestureRecognizers.Any(gesture => gesture is TapGestureRecognizer);

    protected override void OnAttached()
    {
        if (View is null)
        {
            return;
        }

        touchEffect = TouchEffect.PickFrom(Element);

        if (touchEffect?.IsDisabled ?? true)
        {
            return;
        }

        touchEffect.Element = Element as VisualElement;

        View.Touch += OnTouch;
        UpdateClickHandler();

        accessibilityManager = View.Context?.GetSystemService(Context.AccessibilityService) as AccessibilityManager;

        if (accessibilityManager is not null)
        {
            accessibilityListener = new AccessibilityListener(this);
            accessibilityManager.AddAccessibilityStateChangeListener(accessibilityListener);
            accessibilityManager.AddTouchExplorationStateChangeListener(accessibilityListener);
        }

        if (!OperatingSystem.IsAndroidVersionAtLeast((int)BuildVersionCodes.Lollipop) || !touchEffect.NativeAnimation)
        {
            return;
        }

        View.Clickable = true;
        View.LongClickable = true;

        CreateRipple();
        ApplyRipple();

        View.LayoutChange += OnLayoutChange;
    }

    protected override void OnDetached()
    {
        if (touchEffect?.Element is null)
            return;

        try
        {
            if (accessibilityManager is not null && accessibilityListener is not null)
            {
                accessibilityManager.RemoveAccessibilityStateChangeListener(accessibilityListener);
                accessibilityManager.RemoveTouchExplorationStateChangeListener(accessibilityListener);
                accessibilityListener.Dispose();
                accessibilityManager = null;
                accessibilityListener = null;
            }

            RemoveRipple();

            if (View is not null)
            {
                View.LayoutChange -= OnLayoutChange;
                View.Touch -= OnTouch;
                View.Click -= OnClick;
            }

            touchEffect.Element = null;
            touchEffect = null;

            if (rippleView is not null)
            {
                rippleView.Pressed = false;
                ViewGroup?.RemoveView(rippleView);
                rippleView.Dispose();
                rippleView = null;
            }
        }
        catch (ObjectDisposedException)
        {
            // Suppress exception
        }
        isHoverSupported = false;
    }

    protected override void OnElementPropertyChanged(PropertyChangedEventArgs args)
    {
        base.OnElementPropertyChanged(args);

        if (args.PropertyName == TouchEffect.IsEnabledProperty.PropertyName ||
            args.PropertyName == VisualElement.IsEnabledProperty.PropertyName)
        {
            UpdateClickHandler();
        }

        if (args.PropertyName == TouchEffect.NativeAnimationBorderlessProperty.PropertyName)
        {
            CreateRipple();
            ApplyRipple();
        }
    }

    private void UpdateClickHandler()
    {
        if (View is null || !View.IsAlive())
        {
            return;
        }

        View.Click -= OnClick;

        if (touchEffect is null)
        {
            return;
        }

        if (IsAccessibilityMode || (touchEffect.IsEnabled && (touchEffect?.Element?.IsEnabled ?? false)))
        {
            View.Click += OnClick;
            return;
        }
    }

    void OnTouch(object? sender, AView.TouchEventArgs e)
    {
        e.Handled = false;

        if (touchEffect?.IsDisabled ?? true)
            return;

        if (IsAccessibilityMode)
            return;

        switch (e.Event?.ActionMasked)
        {
            case MotionEventActions.Down:
                OnTouchDown(e);
                break;
            case MotionEventActions.Up:
                OnTouchUp();
                break;
            case MotionEventActions.Cancel:
                OnTouchCancel();
                break;
            case MotionEventActions.Move:
                OnTouchMove(sender, e);
                break;
            case MotionEventActions.HoverEnter:
                OnHoverEnter();
                break;
            case MotionEventActions.HoverExit:
                OnHoverExit();
                break;
        }
    }

    void OnTouchDown(AView.TouchEventArgs e)
    {
        _ = e.Event ?? throw new NullReferenceException();

        IsCanceled = false;

        startX = e.Event.GetX();
        startY = e.Event.GetY();

        touchEffect?.HandleUserInteraction(TouchInteractionStatus.Started);
        touchEffect?.HandleTouch(TouchStatus.Started);

        StartRipple(e.Event.GetX(), e.Event.GetY());

        if (touchEffect?.DisallowTouchThreshold > 0)
        {
            ViewGroup?.Parent?.RequestDisallowInterceptTouchEvent(true);
        }
    }

    void OnTouchUp()
        => HandleEnd(touchEffect?.CurrentTouchStatus == TouchStatus.Started ? TouchStatus.Completed : TouchStatus.Cancelled);

    void OnTouchCancel()
        => HandleEnd(TouchStatus.Cancelled);

    void OnTouchMove(object? sender, AView.TouchEventArgs e)
    {
        if (IsCanceled || e.Event == null)
            return;

        var diffX = Math.Abs(e.Event.GetX() - startX) / this.View.Context?.Resources?.DisplayMetrics?.Density ?? throw new NullReferenceException();
        var diffY = Math.Abs(e.Event.GetY() - startY) / this.View.Context?.Resources?.DisplayMetrics?.Density ?? throw new NullReferenceException();
        var maxDiff = Math.Max(diffX, diffY);

        var disallowTouchThreshold = touchEffect?.DisallowTouchThreshold;
        if (disallowTouchThreshold > 0 && maxDiff > disallowTouchThreshold)
        {
            HandleEnd(TouchStatus.Cancelled);
            return;
        }

        if (sender is not AView view)
            return;

        var screenPointerCoords = new Point(view.Left + e.Event.GetX(), view.Top + e.Event.GetY());
        var viewRect = new Rect(view.Left, view.Top, view.Right - view.Left, view.Bottom - view.Top);
        var status = viewRect.Contains(screenPointerCoords) ? TouchStatus.Started : TouchStatus.Cancelled;

        if (isHoverSupported && (status == TouchStatus.Cancelled && touchEffect?.CurrentHoverStatus == HoverStatus.Entered
            || status == TouchStatus.Started && touchEffect?.CurrentHoverStatus == HoverStatus.Exited))
            touchEffect?.HandleHover(status == TouchStatus.Started ? HoverStatus.Entered : HoverStatus.Exited);

        if (touchEffect?.CurrentTouchStatus != status)
        {
            touchEffect?.HandleTouch(status);

            if (status == TouchStatus.Started)
                StartRipple(e.Event.GetX(), e.Event.GetY());
            if (status == TouchStatus.Cancelled)
                EndRipple();
        }
    }

    void OnHoverEnter()
    {
        isHoverSupported = true;
        touchEffect?.HandleHover(HoverStatus.Entered);
    }

    void OnHoverExit()
    {
        isHoverSupported = true;
        touchEffect?.HandleHover(HoverStatus.Exited);
    }

    void OnClick(object? sender, EventArgs args)
    {
        if (touchEffect?.IsDisabled ?? true)
            return;

        if (!IsAccessibilityMode)
            return;

        IsCanceled = false;
        HandleEnd(TouchStatus.Completed);
    }

    void HandleEnd(TouchStatus status)
    {
        if (IsCanceled)
            return;

        IsCanceled = true;
        if (touchEffect?.DisallowTouchThreshold > 0)
            ViewGroup?.Parent?.RequestDisallowInterceptTouchEvent(false);

        touchEffect?.HandleTouch(status);

        touchEffect?.HandleUserInteraction(TouchInteractionStatus.Completed);

        EndRipple();
    }

    #region Native Animation

    private void StartRipple(float x, float y)
    {
        if (touchEffect is null || touchEffect.IsDisabled || !touchEffect.NativeAnimation)
        {
            return;
        }

        if (touchEffect.CanExecute)
        {
            UpdateRipple(touchEffect.NativeAnimationColor ?? defaultNativeAnimationColor);
            if (rippleView is not null)
            {
                rippleView.Enabled = true;
                rippleView.BringToFront();
                ripple?.SetHotspot(x, y);
                rippleView.Pressed = true;
            }
            else if (IsForegroundRippleWithTapGestureRecognizer && View is not null)
            {
                ripple?.SetHotspot(x, y);
                View.Pressed = true;
            }
        }
        else if (rippleView is null)
        {
            UpdateRipple(Colors.Transparent);
        }
    }

    void EndRipple()
    {
        if (touchEffect?.IsDisabled ?? true)
        {
            return;
        }

        if (rippleView is not null)
        {
            if (rippleView.Pressed)
            {
                rippleView.Pressed = false;
                rippleView.Enabled = false;
            }
        }
        else if (IsForegroundRippleWithTapGestureRecognizer)
        {
            if (View.Pressed)
            {
                View.Pressed = false;
            }
        }
    }

    private void CreateRipple()
    {
        if (touchEffect is null)
        {
            return;
        }

        RemoveRipple();

        var drawable = isAtLeastM && ViewGroup == null
                ? View?.Foreground
                : View?.Background;

        var isBorderLess = touchEffect.NativeAnimationBorderless;
        var isEmptyDrawable = Element is Layout || drawable is null;
        var color = touchEffect.NativeAnimationColor ?? defaultNativeAnimationColor;

        if (drawable is RippleDrawable rippleDrawable && rippleDrawable.GetConstantState() is Drawable.ConstantState constantState)
        {
            ripple = (RippleDrawable)constantState.NewDrawable();
        }
        else
        {
            var content = isEmptyDrawable || isBorderLess ? null : drawable;
            var mask = isEmptyDrawable && !isBorderLess ? new ColorDrawable(Color.White) : null;

            ripple = new RippleDrawable(GetColorStateList(color), content, mask);
        }

        UpdateRipple(color);
    }

    private void RemoveRipple()
    {
        if (ripple is null)
        {
            return;
        }

        if (View is not null)
        {
            if (isAtLeastM && View.Foreground == ripple)
            {
                View.Foreground = null;
            }
            else if (View.Background == ripple)
            {
                View.Background = null;
            }
        }

        if (rippleView is not null)
        {
            rippleView.Foreground = null;
            rippleView.Background = null;
        }

        ripple.Dispose();
        ripple = null;
    }

    private void UpdateRipple(MColor color)
    {
        if (touchEffect?.IsDisabled ?? true)
            return;

        if (color == rippleColor && touchEffect.NativeAnimationRadius == rippleRadius)
            return;

        rippleColor = color;
        rippleRadius = touchEffect.NativeAnimationRadius;
        ripple?.SetColor(GetColorStateList(color));
        if (isAtLeastM && ripple is not null)
        {
            ripple.Radius = (int)(View?.Context?.Resources?.DisplayMetrics?.Density * touchEffect.NativeAnimationRadius ?? throw new NullReferenceException("Could not set ripple radius"));
        }
    }

    ColorStateList GetColorStateList(MColor? color)
    {
        var animationColor = color;
        animationColor ??= defaultNativeAnimationColor;

        return new ColorStateList(
            new[] { Array.Empty<int>() },
            new[] { (int)animationColor.ToPlatform() });
    }

    private void ApplyRipple()
    {
        if (ripple is null || touchEffect is null)
        {
            return;
        }

        var isBorderless = touchEffect.NativeAnimationBorderless;

        if (ViewGroup is null)
        {
            if (isAtLeastM)
            {
                View.Foreground = ripple;
            }
            else
            {
                View.Background = ripple;
            }

            return;
        }

        if (rippleView is null)
        {
            rippleView = new FrameLayout(ViewGroup.Context ?? throw new NullReferenceException())
            {
                LayoutParameters = new ViewGroup.LayoutParams(-1, -1),
                Clickable = false,
                Focusable = false,
                Enabled = false
            };

            //ViewGroup.AddView(rippleView);
            //ViewGroup.BringChildToFront(rippleView);

            //System.Diagnostics.Debug.WriteLine(ViewGroup.ChildCount);

            if (Element is ContentView contentView)
            {
                var childTouch = TouchEffect.GetFrom(contentView.Content as BindableObject);

                if (childTouch is null)
                {
                    ViewGroup.AddView(rippleView);
                    ViewGroup.BringChildToFront(rippleView);
                }
            }
            else
            {
                ViewGroup.AddView(rippleView);
                ViewGroup.BringChildToFront(rippleView);
            }
        }

        ViewGroup.SetClipChildren(!isBorderless);

        if (isBorderless)
        {
            rippleView.Background = null;
            rippleView.Foreground = ripple;
        }
        else
        {
            rippleView.Foreground = null;
            rippleView.Background = ripple;
        }
    }

    void OnLayoutChange(object? sender, AView.LayoutChangeEventArgs e)
    {
        if (sender is not AView view || rippleView == null)
            return;

        rippleView.Right = view.Width;
        rippleView.Bottom = view.Height;
    }

    #endregion Native Animation

    private sealed class AccessibilityListener : Java.Lang.Object,
        AccessibilityManager.IAccessibilityStateChangeListener,
        AccessibilityManager.ITouchExplorationStateChangeListener
    {
        PlatformTouchEffect? platformTouchEffect;

        internal AccessibilityListener(PlatformTouchEffect platformTouchEffect)
            => this.platformTouchEffect = platformTouchEffect;

        public void OnAccessibilityStateChanged(bool enabled)
            => platformTouchEffect?.UpdateClickHandler();

        public void OnTouchExplorationStateChanged(bool enabled)
            => platformTouchEffect?.UpdateClickHandler();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                platformTouchEffect = null;
            }

            base.Dispose(disposing);
        }
    }
}