using System.Diagnostics.CodeAnalysis;

using Maui.TouchEffect.Enums;
using Maui.TouchEffect.Extensions;
using static System.Math;

namespace Maui.TouchEffect;

internal sealed class GestureManager : IDisposable, IAsyncDisposable
{
    private Color? defaultBackgroundColor;

    CancellationTokenSource? longPressTokenSource, animationTokenSource;

    public void Dispose()
    {
        animationTokenSource?.Cancel();
        animationTokenSource?.Dispose();

        longPressTokenSource?.Cancel();
        longPressTokenSource?.Dispose();

        longPressTokenSource = animationTokenSource = null;
    }

    public async ValueTask DisposeAsync()
    {
        await(animationTokenSource?.CancelAsync() ?? Task.CompletedTask);
        animationTokenSource?.Dispose();

        await(longPressTokenSource?.CancelAsync() ?? Task.CompletedTask);
        longPressTokenSource?.Dispose();

        longPressTokenSource = animationTokenSource = null;
    }

    internal static void HandleUserInteraction(in TouchEffect touchEffect, in TouchInteractionStatus interactionStatus)
    {
        touchEffect.CurrentInteractionStatus = interactionStatus;
        touchEffect.RaiseInteractionStatusChanged();
    }

    internal static void HandleHover(in TouchEffect touchEffect, in HoverStatus hoverStatus)
    {
        if (!touchEffect.IsEnabled)
        {
            return;
        }

        var hoverState = hoverStatus switch
        {
            HoverStatus.Entered => HoverState.Hovered,
            HoverStatus.Exited => HoverState.Default,
            _ => throw new NotSupportedException($"{nameof(HoverStatus)} {hoverStatus} not yet supported")
        };

        if (touchEffect.CurrentHoverState != hoverState)
        {
            touchEffect.CurrentHoverState = hoverState;
            touchEffect.RaiseHoverStateChanged();
        }

        if (touchEffect.CurrentHoverStatus != hoverStatus)
        {
            touchEffect.CurrentHoverStatus = hoverStatus;
            touchEffect.RaiseHoverStatusChanged();
        }
    }

    internal void HandleTouch(in TouchEffect touchEffect, TouchStatus status)
    {
        if (!touchEffect.IsEnabled)
        {
            return;
        }

        var canExecuteAction = touchEffect.CanExecute;
        if (status != TouchStatus.Started || canExecuteAction)
        {
            if (!canExecuteAction)
            {
                status = TouchStatus.Cancelled;
            }

            var state = status == TouchStatus.Started
                ? TouchState.Pressed
                : TouchState.Default;

            if (status == TouchStatus.Started)
            {
                _animationProgress = 0;
                _animationState = state;
            }

            var isToggled = touchEffect.IsToggled;
            if (isToggled.HasValue)
            {
                if (status != TouchStatus.Started)
                {
                    _durationMultiplier = _animationState == TouchState.Pressed && !isToggled.Value ||
                                        _animationState == TouchState.Default && isToggled.Value
                        ? 1 - _animationProgress
                        : _animationProgress;

                    UpdateStatusAndState(touchEffect, status, state);

                    if (status == TouchStatus.Cancelled)
                    {
                        touchEffect.ForceUpdateState(false);
                        return;
                    }

                    OnTapped(touchEffect);
                    touchEffect.IsToggled = !isToggled;
                    return;
                }

                state = isToggled.Value
                    ? TouchState.Default
                    : TouchState.Pressed;
            }

            UpdateStatusAndState(touchEffect, status, state);
        }

        if (status == TouchStatus.Completed)
        {
            OnTapped(touchEffect);
        }
    }

    internal async Task ChangeStateAsync(TouchEffect touchEffect, bool animated)
    {
        var status = touchEffect.CurrentTouchStatus;
        var state = touchEffect.CurrentTouchState;
        var hoverState = touchEffect.CurrentHoverState;

        AbortAnimations(touchEffect);
        animationTokenSource = new CancellationTokenSource();
        var token = animationTokenSource.Token;

        var isToggled = touchEffect.IsToggled;

        if (touchEffect.Element is not null)
        {
            UpdateVisualState(touchEffect.Element, state, hoverState);
        }

        if (!animated)
        {
            if (isToggled.HasValue)
            {
                state = isToggled.Value
                    ? TouchState.Pressed
                    : TouchState.Default;
            }

            var durationMultiplier = _durationMultiplier;
            _durationMultiplier = null;

            await RunAnimationTask(touchEffect, state, hoverState, animationTokenSource.Token, durationMultiplier.GetValueOrDefault()).ConfigureAwait(false);
            return;
        }

        var pulseCount = touchEffect.PulseCount;

        if (pulseCount == 0 || state == TouchState.Default && !isToggled.HasValue)
        {
            if (isToggled.HasValue)
            {
                state =
                    status == TouchStatus.Started && isToggled.Value ||
                    status != TouchStatus.Started && !isToggled.Value
                        ? TouchState.Default
                        : TouchState.Pressed;
            }

            await RunAnimationTask(touchEffect, state, hoverState, animationTokenSource.Token).ConfigureAwait(false);
            return;
        }

        do
        {
            var rippleState = isToggled.HasValue && isToggled.Value
                ? TouchState.Default
                : TouchState.Pressed;

            await RunAnimationTask(touchEffect, rippleState, hoverState, animationTokenSource.Token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            rippleState = isToggled.HasValue && isToggled.Value
                ? TouchState.Pressed
                : TouchState.Default;

            await RunAnimationTask(touchEffect, rippleState, hoverState, animationTokenSource.Token);
            if (token.IsCancellationRequested)
            {
                return;
            }
        } while (--pulseCount != 0);
    }

    internal void HandleLongPress(TouchEffect touchEffect)
    {
        if (touchEffect.CurrentTouchState == TouchState.Default)
        {
            longPressTokenSource?.Cancel();
            longPressTokenSource?.Dispose();
            longPressTokenSource = null;
            return;
        }

        if (touchEffect.LongPressCommand == null || touchEffect.CurrentInteractionStatus == TouchInteractionStatus.Completed)
        {
            return;
        }

        longPressTokenSource = new CancellationTokenSource();
        _ = Task.Delay(touchEffect.LongPressDuration, longPressTokenSource.Token).ContinueWith(t =>
        {
            if (t.IsFaulted && t.Exception != null)
            {
                throw t.Exception;
            }

            if (t.IsCanceled)
            {
                return;
            }

            var longPressAction = new Action(() =>
            {
                touchEffect.HandleUserInteraction(TouchInteractionStatus.Completed);
                touchEffect.RaiseLongPressCompleted();
            });

            if (MainThread.IsMainThread)
            {
                MainThread.BeginInvokeOnMainThread(longPressAction);
            }
            else
            {
                longPressAction.Invoke();
            }
        });
    }

    internal void Reset()
    {
        SetCustomAnimationTask(null);
        defaultBackgroundColor = default;
    }

    internal void AbortAnimations(TouchEffect touchEffect)
    {
        animationTokenSource?.Cancel();
        animationTokenSource?.Dispose();
        animationTokenSource = null;
        var element = touchEffect.Element;
        if (element == null)
        {
            return;
        }

        element.AbortAnimations();
    }

    internal void OnTapped(TouchEffect touchEffect)
    {
        if (!touchEffect.CanExecute || (touchEffect.LongPressCommand is not null && touchEffect.CurrentInteractionStatus == TouchInteractionStatus.Completed))
        {
            return;
        }

#if ANDROID
        HandleCollectionViewSelection(touchEffect);
#endif

        if (touchEffect.Element is IButtonController button)
        {
            button.SendClicked();
        }

        touchEffect.RaiseGestureCompleted();
    }

#if ANDROID
    static void HandleCollectionViewSelection(in TouchEffect touchEffect)
    {
        if (touchEffect.Element is null
            || !TryFindParentElementWithParentOfType(touchEffect.Element, out var child, out CollectionView? collectionView))
        {
            return;
        }

        var selectedItem = child.BindingContext;

        switch (collectionView.SelectionMode)
        {
            case SelectionMode.Single:
                collectionView.SelectedItem = selectedItem;
                break;

            case SelectionMode.Multiple:
                var selectedItems = collectionView.SelectedItems?.ToList() ?? [];

                if (!selectedItems.Remove(selectedItem))
                {
                    selectedItems.Add(selectedItem);
                }

                collectionView.UpdateSelectedItems(selectedItems);
                break;

            case SelectionMode.None:
                break;

            default:
                throw new NotSupportedException($"{nameof(SelectionMode)} {collectionView.SelectionMode} is not yet supported");
        }

        static bool TryFindParentElementWithParentOfType<T>(in VisualElement element, [NotNullWhen(true)] out VisualElement? child, [NotNullWhen(true)] out T? parent) where T : VisualElement
        {
            ArgumentNullException.ThrowIfNull(element);

            VisualElement? searchingElement = element;

            child = null;
            parent = null;

            while (searchingElement?.Parent is not null)
            {
                if (searchingElement.Parent is not T parentElement)
                {
                    searchingElement = searchingElement.Parent as VisualElement;
                    continue;
                }

                child = searchingElement;
                parent = parentElement;

                return true;
            }

            return false;
        }
    }
#endif

    private static async Task SetImageSource(TouchEffect touchEffect, TouchState touchState, HoverState hoverState, TimeSpan duration, CancellationToken token)
    {
        if (touchEffect.Element is not Image image)
        {
            return;
        }

        var normalBackgroundImageSource = touchEffect.DefaultImageSource;
        var pressedBackgroundImageSource = touchEffect.PressedImageSource;
        var hoveredBackgroundImageSource = touchEffect.HoveredImageSource;

        if (normalBackgroundImageSource == null &&
            pressedBackgroundImageSource == null &&
            hoveredBackgroundImageSource == null)
        {
            return;
        }

        try
        {
            if (touchEffect.ShouldSetImageOnAnimationEnd && duration > TimeSpan.Zero)
            {
                await Task.Delay(duration, token);
            }
        }
        catch (TaskCanceledException)
        {
            return;
        }

        switch (touchState, hoverState)
        {
            case (TouchState.Pressed, _):
                if (touchEffect.Element.IsSet(TouchEffect.PressedImageAspectProperty))
                {
                    image.Aspect = touchEffect.PressedImageAspect;
                }

                if (touchEffect.Element.IsSet(TouchEffect.PressedImageSourceProperty))
                {
                    image.Source = touchEffect.PressedImageSource;
                }
                break;

            case (TouchState.Default, HoverState.Hovered):
                if (touchEffect.Element.IsSet(TouchEffect.HoveredImageAspectProperty))
                {
                    image.Aspect = touchEffect.HoveredImageAspect;
                }
                else if (touchEffect.Element.IsSet(TouchEffect.DefaultImageAspectProperty))
                {
                    image.Aspect = touchEffect.DefaultImageAspect;
                }

                if (touchEffect.Element.IsSet(TouchEffect.HoveredImageSourceProperty))
                {
                    image.Source = touchEffect.HoveredImageSource;
                }
                else if (touchEffect.Element.IsSet(TouchEffect.DefaultImageSourceProperty))
                {
                    image.Source = touchEffect.DefaultImageSource;
                }

                break;

            case (TouchState.Default, HoverState.Default):
                if (touchEffect.Element.IsSet(TouchEffect.DefaultImageAspectProperty))
                {
                    image.Aspect = touchEffect.DefaultImageAspect;
                }

                if (touchEffect.Element.IsSet(TouchEffect.DefaultImageSourceProperty))
                {
                    image.Source = touchEffect.DefaultImageSource;
                }
                break;

            default:
                throw new NotSupportedException($"The combination of {nameof(TouchState)} {touchState} and {nameof(HoverState)} {hoverState} is not yet supported");
        }
    }

    private static void UpdateStatusAndState(TouchEffect sender, TouchStatus status, TouchState state)
    {
        sender.CurrentTouchStatus = status;
        sender.RaiseCurrentTouchStatusChanged();

        if (sender.CurrentTouchState != state || status != TouchStatus.Cancelled)
        {
            sender.CurrentTouchState = state;
            sender.RaiseCurrentTouchStateChanged();
        }
    }

    private static void UpdateVisualState(VisualElement visualElement, TouchState touchState, HoverState hoverState)
    {
        var state = (touchState, hoverState) switch
        {
            (TouchState.Pressed, _) => TouchEffect.PressedVisualState,
            (TouchState.Default, HoverState.Hovered) => TouchEffect.HoveredVisualState,
            (TouchState.Default, HoverState.Default) => TouchEffect.UnpressedVisualState,
            _ => throw new NotSupportedException($"The combination of {nameof(TouchState)} {touchState} and {nameof(HoverState)} {hoverState} is not yet supported")
        };

        VisualStateManager.GoToState(visualElement, state);
    }

    private static Task? SetOpacity(TouchEffect sender, TouchState touchState, HoverState hoverState, int duration, Easing easing)
    {
        var normalOpacity = sender.DefaultOpacity;
        var pressedOpacity = sender.PressedOpacity;
        var hoveredOpacity = sender.HoveredOpacity;

        if (Abs(normalOpacity - 1) <= double.Epsilon &&
            Abs(pressedOpacity - 1) <= double.Epsilon &&
            Abs(hoveredOpacity - 1) <= double.Epsilon)
        {
            return Task.FromResult(false);
        }

        var opacity = normalOpacity;

        if (touchState == TouchState.Pressed)
        {
            opacity = pressedOpacity;
        }
        else if (hoverState == HoverState.Hovered && (sender.Element?.IsSet(TouchEffect.HoveredOpacityProperty) ?? false))
        {
            opacity = hoveredOpacity;
        }

        var element = sender.Element;
        if (duration <= 0 && element != null)
        {
            element.AbortAnimations();
            element.Opacity = opacity;
            return Task.FromResult(true);
        }

        return element?.FadeTo(opacity, (uint)Abs(duration), easing);
    }

    private Task SetScale(TouchEffect sender, TouchState touchState, HoverState hoverState, int duration, Easing easing)
    {
        var normalScale = sender.DefaultScale;
        var pressedScale = sender.PressedScale;
        var hoveredScale = sender.HoveredScale;

        if (Abs(normalScale - 1) <= double.Epsilon &&
            Abs(pressedScale - 1) <= double.Epsilon &&
            Abs(hoveredScale - 1) <= double.Epsilon)
        {
            return Task.FromResult(false);
        }

        var scale = normalScale;

        if (touchState == TouchState.Pressed)
        {
            scale = pressedScale;
        }
        else if (hoverState == HoverState.Hovered && (sender.Element?.IsSet(TouchEffect.HoveredScaleProperty) ?? false))
        {
            scale = hoveredScale;
        }

        var element = sender.Element;
        if (element == null)
        {
            return Task.FromResult(false);
        }

        if (duration <= 0)
        {
            element.AbortAnimations(nameof(SetScale));
            element.Scale = scale;
            return Task.FromResult(true);
        }

        var animationCompletionSource = new TaskCompletionSource<bool>();
        element.Animate(nameof(SetScale), v =>
        {
            if (double.IsNaN(v))
            {
                return;
            }

            element.Scale = v;
        }, element.Scale, scale, 16, (uint)Abs(duration), easing, (v, b) => animationCompletionSource.SetResult(b));
        return animationCompletionSource.Task;
    }

    private static Task SetTranslation(TouchEffect sender, TouchState touchState, HoverState hoverState, int duration, Easing easing)
    {
        var normalTranslationX = sender.DefaultTranslationX;
        var pressedTranslationX = sender.PressedTranslationX;
        var hoveredTranslationX = sender.HoveredTranslationX;

        var normalTranslationY = sender.DefaultTranslationY;
        var pressedTranslationY = sender.PressedTranslationY;
        var hoveredTranslationY = sender.HoveredTranslationY;

        if (Abs(normalTranslationX) <= double.Epsilon
            && Abs(pressedTranslationX) <= double.Epsilon
            && Abs(hoveredTranslationX) <= double.Epsilon
            && Abs(normalTranslationY) <= double.Epsilon
            && Abs(pressedTranslationY) <= double.Epsilon
            && Abs(hoveredTranslationY) <= double.Epsilon)
        {
            return Task.FromResult(false);
        }

        var translationX = normalTranslationX;
        var translationY = normalTranslationY;

        if (touchState == TouchState.Pressed)
        {
            translationX = pressedTranslationX;
            translationY = pressedTranslationY;
        }
        else if (hoverState == HoverState.Hovered)
        {
            if (sender.Element?.IsSet(TouchEffect.HoveredTranslationXProperty) ?? false)
            {
                translationX = hoveredTranslationX;
            }

            if (sender.Element?.IsSet(TouchEffect.HoveredTranslationYProperty) ?? false)
            {
                translationY = hoveredTranslationY;
            }
        }

        var element = sender.Element;
        if (duration <= 0 && element != null)
        {
            element.AbortAnimations();
            element.TranslationX = translationX;
            element.TranslationY = translationY;
            return Task.FromResult(true);
        }

        return element?.TranslateTo(translationX, translationY, (uint)Abs(duration), easing) ?? Task.FromResult(false);
    }

    private static Task SetRotation(TouchEffect sender, TouchState touchState, HoverState hoverState, int duration, Easing easing)
    {
        var normalRotation = sender.DefaultRotation;
        var pressedRotation = sender.PressedRotation;
        var hoveredRotation = sender.HoveredRotation;

        if (Abs(normalRotation) <= double.Epsilon
            && Abs(pressedRotation) <= double.Epsilon
            && Abs(hoveredRotation) <= double.Epsilon)
        {
            return Task.FromResult(false);
        }

        var rotation = normalRotation;

        if (touchState == TouchState.Pressed)
        {
            rotation = pressedRotation;
        }
        else if (hoverState == HoverState.Hovered && (sender.Element?.IsSet(TouchEffect.HoveredRotationProperty) ?? false))
        {
            rotation = hoveredRotation;
        }

        var element = sender.Element;
        if (duration <= 0 && element != null)
        {
            element.AbortAnimations();
            element.Rotation = rotation;
            return Task.FromResult(true);
        }

        return element?.RotateTo(rotation, (uint)Abs(duration), easing) ?? Task.FromResult(false);
    }

    private static Task SetRotationX(TouchEffect sender, TouchState touchState, HoverState hoverState, int duration, Easing easing)
    {
        var normalRotationX = sender.DefaultRotationX;
        var pressedRotationX = sender.PressedRotationX;
        var hoveredRotationX = sender.HoveredRotationX;

        if (Abs(normalRotationX) <= double.Epsilon &&
            Abs(pressedRotationX) <= double.Epsilon &&
            Abs(hoveredRotationX) <= double.Epsilon)
        {
            return Task.FromResult(false);
        }

        var rotationX = normalRotationX;

        if (touchState == TouchState.Pressed)
        {
            rotationX = pressedRotationX;
        }
        else if (hoverState == HoverState.Hovered && (sender.Element?.IsSet(TouchEffect.HoveredRotationXProperty) ?? false))
        {
            rotationX = hoveredRotationX;
        }

        var element = sender.Element;
        if (duration <= 0 && element != null)
        {
            element.AbortAnimations();
            element.RotationX = rotationX;
            return Task.FromResult(true);
        }

        return element?.RotateXTo(rotationX, (uint)Abs(duration), easing) ?? Task.FromResult(false);
    }

    private static Task SetRotationY(TouchEffect sender, TouchState touchState, HoverState hoverState, int duration, Easing easing)
    {
        var normalRotationY = sender.DefaultRotationY;
        var pressedRotationY = sender.PressedRotationY;
        var hoveredRotationY = sender.HoveredRotationY;

        if (Abs(normalRotationY) <= double.Epsilon &&
            Abs(pressedRotationY) <= double.Epsilon &&
            Abs(hoveredRotationY) <= double.Epsilon)
        {
            return Task.FromResult(false);
        }

        var rotationY = normalRotationY;

        if (touchState == TouchState.Pressed)
        {
            rotationY = pressedRotationY;
        }
        else if (hoverState == HoverState.Hovered && (sender.Element?.IsSet(TouchEffect.HoveredRotationYProperty) ?? false))
        {
            rotationY = hoveredRotationY;
        }

        var element = sender.Element;
        if (duration <= 0 && element != null)
        {
            element.AbortAnimations();
            element.RotationY = rotationY;
            return Task.FromResult(true);
        }

        return element?.RotateYTo(rotationY, (uint)Abs(duration), easing) ?? Task.FromResult(false);
    }

    private async Task<bool> SetBackgroundColor(TouchEffect touchEffect, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing easing)
    {
        if (touchEffect.Element is not VisualElement)
        {
            return false;
        }

        var normalBackgroundColor = touchEffect.DefaultBackgroundColor;
        var pressedBackgroundColor = touchEffect.PressedBackgroundColor;
        var hoveredBackgroundColor = touchEffect.HoveredBackgroundColor;

        if (touchEffect.Element == null
            || normalBackgroundColor is null
            && pressedBackgroundColor is null
            && hoveredBackgroundColor is null)
        {
            return false;
        }

        var element = touchEffect.Element;

        defaultBackgroundColor ??= element.BackgroundColor ?? normalBackgroundColor;

        var updatedBackgroundColor = defaultBackgroundColor;

        switch (touchState, hoverState)
        {
            case (TouchState.Pressed, _):
                if (touchEffect.Element.IsSet(TouchEffect.PressedBackgroundColorProperty))
                {
                    updatedBackgroundColor = touchEffect.PressedBackgroundColor;
                }
                break;

            case (TouchState.Default, HoverState.Hovered):
                if (touchEffect.Element.IsSet(TouchEffect.HoveredBackgroundColorProperty))
                {
                    updatedBackgroundColor = touchEffect.HoveredBackgroundColor;
                }
                else if (touchEffect.Element.IsSet(TouchEffect.DefaultBackgroundColorProperty))
                {
                    updatedBackgroundColor = touchEffect.DefaultBackgroundColor;
                }
                break;

            case (TouchState.Default, HoverState.Default):
                if (touchEffect.Element.IsSet(TouchEffect.DefaultBackgroundColorProperty))
                {
                    updatedBackgroundColor = touchEffect.DefaultBackgroundColor;
                }
                break;

            default:
                throw new NotSupportedException($"The combination of {nameof(TouchState)} {touchState} and {nameof(HoverState)} {hoverState} is not yet supported");
        }

        if (duration <= TimeSpan.Zero)
        {
            element.AbortAnimations();
            element.BackgroundColor = updatedBackgroundColor;

            return true;
        }

        return await element.BackgroundColorTo(updatedBackgroundColor ?? Colors.Transparent, length: (uint)duration.TotalMilliseconds, easing: easing);
    }

    private Task RunAnimationTask(TouchEffect sender, TouchState touchState, HoverState hoverState, CancellationToken token, double? durationMultiplier = null)
    {
        if (sender.Element == null)
        {
            return Task.FromResult(false);
        }

        var duration = sender.DefaultAnimationDuration;
        var easing = sender.DefaultAnimationEasing;

        if (touchState == TouchState.Pressed)
        {
            if (sender.Element.IsSet(TouchEffect.PressedAnimationDurationProperty))
            {
                duration = sender.PressedAnimationDuration;
            }

            if (sender.Element.IsSet(TouchEffect.PressedAnimationEasingProperty))
            {
                easing = sender.PressedAnimationEasing;
            }
        }
        else if (hoverState == HoverState.Hovered)
        {
            if (sender.Element.IsSet(TouchEffect.HoveredAnimationDurationProperty))
            {
                duration = sender.HoveredAnimationDuration;
            }

            if (sender.Element.IsSet(TouchEffect.HoveredAnimationEasingProperty))
            {
                easing = sender.HoveredAnimationEasing;
            }
        }
        else
        {
            if (sender.Element.IsSet(TouchEffect.DefaultAnimationDurationProperty))
            {
                duration = sender.DefaultAnimationDuration;
            }

            if (sender.Element.IsSet(TouchEffect.DefaultAnimationEasingProperty))
            {
                easing = sender.DefaultAnimationEasing;
            }
        }

        if (durationMultiplier.HasValue)
        {
            duration = (int)durationMultiplier.Value * duration;
        }

        duration = Max(duration, 0);

        return Task.WhenAll(
            _animationTaskFactory?.Invoke(sender, touchState, hoverState, duration, easing, token) ?? Task.FromResult(true),
            SetImageSource(sender, touchState, hoverState, TimeSpan.FromMilliseconds(duration), token),
            SetBackgroundColor(sender, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing),
            SetOpacity(sender, touchState, hoverState, duration, easing),
            SetScale(sender, touchState, hoverState, duration, easing),
            SetTranslation(sender, touchState, hoverState, duration, easing),
            SetRotation(sender, touchState, hoverState, duration, easing),
            SetRotationX(sender, touchState, hoverState, duration, easing),
            SetRotationY(sender, touchState, hoverState, duration, easing),
            Task.Run(async () =>
            {
                _animationProgress = 0;
                _animationState = touchState;

                for (var progress = _animationProgressDelay; progress < duration; progress += _animationProgressDelay)
                {
                    await Task.Delay(_animationProgressDelay).ConfigureAwait(false);
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    _animationProgress = (double)progress / duration;
                }

                _animationProgress = 1;
            }));
    }

    private const int _animationProgressDelay = 10;
#pragma warning disable IDE1006 // Naming Styles
	private Func<TouchEffect, TouchState, HoverState, int, Easing, CancellationToken, Task>? _animationTaskFactory;
	private double? _durationMultiplier;
	private double _animationProgress;
	private TouchState? _animationState;
#pragma warning restore IDE1006 // Naming Styles

	internal void SetCustomAnimationTask(Func<TouchEffect, TouchState, HoverState, int, Easing, CancellationToken, Task>? animationTaskFactory)
	{
		_animationTaskFactory = animationTaskFactory;
	}
}
