using Android.Content;
using Android.Content.Res;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Views.Accessibility;
using Android.Widget;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using System.ComponentModel;
using AView = Android.Views.View;
using Color = Android.Graphics.Color;
using Mview = Microsoft.Maui.Controls.View;
using Mcolor = Microsoft.Maui.Graphics.Color;
using Maui.TouchEffect.Enums;
using Microsoft.Maui.Platform;

namespace Maui.TouchEffect;

public class PlatformTouchEffect : Microsoft.Maui.Controls.Platform.PlatformEffect
{
    private static readonly Mcolor defaultNativeAnimationColor = new(128, 128, 128, 64);

    AccessibilityManager? accessibilityManager;
    AccessibilityListener? accessibilityListener;
    TouchEffect? effect;
    bool isHoverSupported;
    RippleDrawable? ripple;
    AView? rippleView;
    float startX;
    float startY;
#pragma warning disable IDE1006
    Mcolor? _rippleColor;
    int _rippleRadius = -1;
#pragma warning restore IDE1006

    AView _view => Control ?? Container;

    ViewGroup? _group => (Container ?? Control) as ViewGroup;

    internal bool IsCanceled { get; set; }

    bool IsAccessibilityMode => accessibilityManager != null
        && accessibilityManager.IsEnabled
        && accessibilityManager.IsTouchExplorationEnabled;

    bool IsForegroundRippleWithTapGestureRecognizer
        => ripple != null &&
            ripple.IsAlive() &&
            _view.IsAlive() &&
            _view.Foreground == ripple &&
            Element is Mview view &&
            view.GestureRecognizers.Any(gesture => gesture is TapGestureRecognizer);

    protected override void OnAttached()
    {
        if (_view == null)
            return;

        effect = TouchEffect.PickFrom(Element);
        if (effect?.IsDisabled ?? true)
            return;

        effect.Element = (VisualElement)Element;

        _view.Touch += OnTouch;
        UpdateClickHandler();

        accessibilityManager = _view.Context?.GetSystemService(Context.AccessibilityService) as AccessibilityManager;
        if (accessibilityManager != null)
        {
            accessibilityListener = new AccessibilityListener(this);
            accessibilityManager.AddAccessibilityStateChangeListener(accessibilityListener);
            accessibilityManager.AddTouchExplorationStateChangeListener(accessibilityListener);
        }

        if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop || !effect.NativeAnimation)
            return;

        _view.Clickable = true;
        _view.LongClickable = true;
        CreateRipple();

        if (_group == null)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                _view.Foreground = ripple;

            return;
        }

        rippleView = new FrameLayout(_group.Context ?? throw new NullReferenceException())
        {
            LayoutParameters = new ViewGroup.LayoutParams(-1, -1),
            Clickable = false,
            Focusable = false,
            Enabled = false,
        };
        _view.LayoutChange += OnLayoutChange;
        rippleView.Background = ripple;
        _group.AddView(rippleView);
        rippleView.BringToFront();
    }

    protected override void OnDetached()
    {
        if (effect?.Element == null)
            return;

        try
        {
            if (accessibilityManager != null && accessibilityListener != null)
            {
                accessibilityManager.RemoveAccessibilityStateChangeListener(accessibilityListener);
                accessibilityManager.RemoveTouchExplorationStateChangeListener(accessibilityListener);
                accessibilityListener.Dispose();
                accessibilityManager = null;
                accessibilityListener = null;
            }

            if (_view != null)
            {
                _view.LayoutChange -= OnLayoutChange;
                _view.Touch -= OnTouch;
                _view.Click -= OnClick;

                if (Build.VERSION.SdkInt >= BuildVersionCodes.M && _view.Foreground == ripple)
                    _view.Foreground = null;
            }

            effect.Element = null;
            effect = null;

            if (rippleView != null)
            {
                rippleView.Pressed = false;
                rippleView.Background = null;
                _group?.RemoveView(rippleView);
                rippleView.Dispose();
                rippleView = null;
            }

            ripple?.Dispose();
            ripple = null;
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
        if (args.PropertyName == TouchEffect.IsAvailableProperty.PropertyName ||
            args.PropertyName == VisualElement.IsEnabledProperty.PropertyName)
        {
            UpdateClickHandler();
        }
    }

    void UpdateClickHandler()
    {
        _view.Click -= OnClick;
        if (IsAccessibilityMode || (effect?.IsAvailable ?? false) && (effect?.Element?.IsEnabled ?? false))
        {
            _view.Click += OnClick;
            return;
        }
    }

    void OnTouch(object sender, AView.TouchEventArgs e)
    {
        e.Handled = false;

        if (effect?.IsDisabled ?? true)
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

        effect?.HandleUserInteraction(TouchInteractionStatus.Started);
        effect?.HandleTouch(TouchStatus.Started);

        StartRipple(e.Event.GetX(), e.Event.GetY());

        if (effect?.DisallowTouchThreshold > 0)
            _group?.Parent?.RequestDisallowInterceptTouchEvent(true);
    }

    void OnTouchUp()
        => HandleEnd(effect?.Status == TouchStatus.Started ? TouchStatus.Completed : TouchStatus.Canceled);

    void OnTouchCancel()
        => HandleEnd(TouchStatus.Canceled);

    void OnTouchMove(object sender, AView.TouchEventArgs e)
    {
        if (IsCanceled || e.Event == null)
            return;

        var diffX = Math.Abs(e.Event.GetX() - startX) / _view.Context?.Resources?.DisplayMetrics?.Density ?? throw new NullReferenceException();
        var diffY = Math.Abs(e.Event.GetY() - startY) / _view.Context?.Resources?.DisplayMetrics?.Density ?? throw new NullReferenceException();
        var maxDiff = Math.Max(diffX, diffY);

        var disallowTouchThreshold = effect?.DisallowTouchThreshold;
        if (disallowTouchThreshold > 0 && maxDiff > disallowTouchThreshold)
        {
            HandleEnd(TouchStatus.Canceled);
            return;
        }

        if (sender is not AView view)
            return;

        var screenPointerCoords = new Point(view.Left + e.Event.GetX(), view.Top + e.Event.GetY());
        var viewRect = new Rect(view.Left, view.Top, view.Right - view.Left, view.Bottom - view.Top);
        var status = viewRect.Contains(screenPointerCoords) ? TouchStatus.Started : TouchStatus.Canceled;

        if (isHoverSupported && (status == TouchStatus.Canceled && effect?.HoverStatus == HoverStatus.Entered
            || status == TouchStatus.Started && effect?.HoverStatus == HoverStatus.Exited))
            effect?.HandleHover(status == TouchStatus.Started ? HoverStatus.Entered : HoverStatus.Exited);

        if (effect?.Status != status)
        {
            effect?.HandleTouch(status);

            if (status == TouchStatus.Started)
                StartRipple(e.Event.GetX(), e.Event.GetY());
            if (status == TouchStatus.Canceled)
                EndRipple();
        }
    }

    void OnHoverEnter()
    {
        isHoverSupported = true;
        effect?.HandleHover(HoverStatus.Entered);
    }

    void OnHoverExit()
    {
        isHoverSupported = true;
        effect?.HandleHover(HoverStatus.Exited);
    }

    void OnClick(object sender, System.EventArgs args)
    {
        if (effect?.IsDisabled ?? true)
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
        if (effect?.DisallowTouchThreshold > 0)
            _group?.Parent?.RequestDisallowInterceptTouchEvent(false);

        effect?.HandleTouch(status);

        effect?.HandleUserInteraction(TouchInteractionStatus.Completed);

        EndRipple();
    }

    void StartRipple(float x, float y)
    {
        if (effect?.IsDisabled ?? true)
            return;

        if (effect.CanExecute && effect.NativeAnimation)
        {
            UpdateRipple();
            if (rippleView != null)
            {
                rippleView.Enabled = true;
                rippleView.BringToFront();
                ripple?.SetHotspot(x, y);
                rippleView.Pressed = true;
            }
            else if (IsForegroundRippleWithTapGestureRecognizer)
            {
                ripple?.SetHotspot(x, y);
                _view.Pressed = true;
            }
        }
    }

    void EndRipple()
    {
        if (effect?.IsDisabled ?? true)
            return;

        if (rippleView != null)
        {
            if (rippleView.Pressed)
            {
                rippleView.Pressed = false;
                rippleView.Enabled = false;
            }
        }
        else if (IsForegroundRippleWithTapGestureRecognizer)
        {
            if (_view.Pressed)
                _view.Pressed = false;
        }
    }

    void CreateRipple()
    {
        var drawable = Build.VERSION.SdkInt >= BuildVersionCodes.M && _group == null
            ? _view?.Foreground
            : _view?.Background;

        var isEmptyDrawable = Element is Layout || drawable == null;

        if (drawable is RippleDrawable rippleDrawable && rippleDrawable.GetConstantState() is Drawable.ConstantState constantState)
            ripple = (RippleDrawable)constantState.NewDrawable();
        else
            ripple = new RippleDrawable(GetColorStateList(), isEmptyDrawable ? null : drawable, isEmptyDrawable ? new ColorDrawable(Color.White) : null);

        UpdateRipple();
    }

    void UpdateRipple()
    {
        if (effect?.IsDisabled ?? true)
            return;

        if (effect.NativeAnimationColor == _rippleColor && effect.NativeAnimationRadius == _rippleRadius)
            return;

        _rippleColor = effect.NativeAnimationColor;
        _rippleRadius = effect.NativeAnimationRadius;
        ripple?.SetColor(GetColorStateList());
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M && ripple != null)
            ripple.Radius = (int)(_view.Context?.Resources?.DisplayMetrics?.Density * effect?.NativeAnimationRadius ?? throw new NullReferenceException());
    }

    ColorStateList GetColorStateList()
    {
        var nativeAnimationColor = effect?.NativeAnimationColor;
        nativeAnimationColor ??= defaultNativeAnimationColor;

        return new ColorStateList(
            new[] { new int[] { } },
            new[] { (int)nativeAnimationColor.ToPlatform() });
    }

    void OnLayoutChange(object sender, AView.LayoutChangeEventArgs e)
    {
        //if (sender is not AView view || (_group as IVisualElementRenderer)?.Element == null || rippleView == null)
        //    return;

        //rippleView.Right = view.Width;
        //rippleView.Bottom = view.Height;
    }

    sealed class AccessibilityListener : Java.Lang.Object,
                                         AccessibilityManager.IAccessibilityStateChangeListener,
                                         AccessibilityManager.ITouchExplorationStateChangeListener
    {
        PlatformTouchEffect platformTouchEffect;

        internal AccessibilityListener(PlatformTouchEffect platformTouchEffect)
            => this.platformTouchEffect = platformTouchEffect;

        public void OnAccessibilityStateChanged(bool enabled)
            => platformTouchEffect?.UpdateClickHandler();

        public void OnTouchExplorationStateChanged(bool enabled)
            => platformTouchEffect?.UpdateClickHandler();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                platformTouchEffect = null;

            base.Dispose(disposing);
        }
    }
}