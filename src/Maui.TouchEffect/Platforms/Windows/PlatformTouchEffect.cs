using Maui.TouchEffect.Enums;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;

namespace Maui.TouchEffect;

public class PlatformTouchEffect : Microsoft.Maui.Controls.Platform.PlatformEffect
{
    const string pointerDownAnimationKey = "PointerDownAnimation";

    const string pointerUpAnimationKey = "PointerUpAnimation";

    TouchEffect? touchEffect;

    bool isPressed;

    bool isIntentionalCaptureLoss;

    Storyboard? pointerDownStoryboard;

    Storyboard? pointerUpStoryboard;

    protected override void OnAttached()
    {
        touchEffect = TouchEffect.PickFrom(Element);

        if (touchEffect?.IsDisabled ?? true)
        {
            return;
        }

        touchEffect.Element = Element as VisualElement;

        //if (touchEffect.NativeAnimation)
        //{
        //    var nativeControl = Container;
        //    if (string.IsNullOrEmpty(nativeControl.Name))
        //        nativeControl.Name = Guid.NewGuid().ToString();

        //    if (nativeControl.Resources.ContainsKey(pointerDownAnimationKey))
        //    {
        //        pointerDownStoryboard = (Storyboard)nativeControl.Resources[pointerDownAnimationKey];
        //    }
        //    else
        //    {
        //        pointerDownStoryboard = new Storyboard();
        //        var downThemeAnimation = new PointerDownThemeAnimation();

        //        Storyboard.SetTargetName(downThemeAnimation, nativeControl.Name);

        //        pointerDownStoryboard.Children.Add(downThemeAnimation);

        //        nativeControl.Resources.Add(new KeyValuePair<object, object>(pointerDownAnimationKey, pointerDownStoryboard));
        //    }

        //    if (nativeControl.Resources.ContainsKey(pointerUpAnimationKey))
        //    {
        //        pointerUpStoryboard = (Storyboard)nativeControl.Resources[pointerUpAnimationKey];
        //    }
        //    else
        //    {
        //        pointerUpStoryboard = new Storyboard();
        //        var upThemeAnimation = new PointerUpThemeAnimation();

        //        Storyboard.SetTargetName(upThemeAnimation, nativeControl.Name);

        //        pointerUpStoryboard.Children.Add(upThemeAnimation);

        //        nativeControl.Resources.Add(new KeyValuePair<object, object>(pointerUpAnimationKey, pointerUpStoryboard));
        //    }
        //}

        var platformView = Container;

        if (platformView.Resources.ContainsKey(pointerDownAnimationKey))
        {
            pointerDownStoryboard = (Storyboard)platformView.Resources[pointerDownAnimationKey];
        }
        else
        {
            pointerDownStoryboard = new();
            var downThemeAnimation = new PointerDownThemeAnimation();

            Storyboard.SetTargetName(downThemeAnimation, platformView.Name);
            pointerDownStoryboard.Children.Add(downThemeAnimation);
            platformView.Resources.Add(new KeyValuePair<object, object>(pointerDownAnimationKey, pointerDownStoryboard));
        }

        if (platformView.Resources.ContainsKey(pointerUpAnimationKey))
        {
            pointerUpStoryboard = (Storyboard)platformView.Resources[pointerUpAnimationKey];
        }
        else
        {
            pointerUpStoryboard = new();
            var upThemeAnimation = new PointerUpThemeAnimation();

            Storyboard.SetTargetName(upThemeAnimation, platformView.Name);

            pointerUpStoryboard.Children.Add(upThemeAnimation);

            platformView.Resources.Add(new KeyValuePair<object, object>(pointerUpAnimationKey, pointerUpStoryboard));
        }

        Container.PointerPressed += OnPointerPressed;
        Container.PointerReleased += OnPointerReleased;
        Container.PointerCanceled += OnPointerCanceled;
        Container.PointerExited += OnPointerExited;
        Container.PointerEntered += OnPointerEntered;
        Container.PointerCaptureLost += OnPointerCaptureLost;
    }

    protected override void OnDetached()
    {
        if (touchEffect?.Element is null)
            return;

        touchEffect.Element = null;
        touchEffect = null;

        Container.PointerPressed -= OnPointerPressed;
        Container.PointerReleased -= OnPointerReleased;
        Container.PointerCanceled -= OnPointerCanceled;
        Container.PointerExited -= OnPointerExited;
        Container.PointerEntered -= OnPointerEntered;
        Container.PointerCaptureLost -= OnPointerCaptureLost;

        isPressed = false;
    }

    void OnPointerEntered(object? sender, PointerRoutedEventArgs e)
    {
        if (touchEffect is null || touchEffect.Element is null || !touchEffect.IsEnabled)
        {
            return;
        }

        touchEffect.HandleHover(HoverStatus.Entered);

        if (isPressed)
        {
            touchEffect.HandleTouch(TouchStatus.Started);
            AnimateTilt(pointerDownStoryboard);
        }
    }

    void OnPointerExited(object? sender, PointerRoutedEventArgs e)
    {
        if (touchEffect is null || touchEffect.Element is null || !touchEffect.IsEnabled)
        {
            return;
        }

        if (isPressed)
        {
            touchEffect.HandleTouch(TouchStatus.Cancelled);
            AnimateTilt(pointerUpStoryboard);
        }

        touchEffect.HandleHover(HoverStatus.Exited);
    }

    void OnPointerCanceled(object? sender, PointerRoutedEventArgs e)
    {
        if (touchEffect is null || touchEffect.Element is null || !touchEffect.IsEnabled)
        {
            return;
        }

        isPressed = false;

        touchEffect.HandleTouch(TouchStatus.Cancelled);
        touchEffect.HandleUserInteraction(TouchInteractionStatus.Completed);
        touchEffect.HandleHover(HoverStatus.Exited);

        AnimateTilt(pointerUpStoryboard);
    }

    void OnPointerCaptureLost(object? sender, PointerRoutedEventArgs e)
    {
        if (touchEffect is null || touchEffect.Element is null || !touchEffect.IsEnabled)
        {
            return;
        }

        if (isIntentionalCaptureLoss)
        {
            return;
        }

        isPressed = false;

        if (touchEffect.CurrentTouchStatus is not TouchStatus.Cancelled)
        {
            touchEffect.HandleTouch(TouchStatus.Cancelled);
        }

        touchEffect.HandleUserInteraction(TouchInteractionStatus.Completed);

        if (touchEffect.CurrentHoverStatus is not HoverStatus.Exited)
        { 
            touchEffect.HandleHover(HoverStatus.Exited);
        }

        AnimateTilt(pointerUpStoryboard);
    }

    void OnPointerReleased(object? sender, PointerRoutedEventArgs e)
    {
        if (touchEffect is null || touchEffect.Element is null || !touchEffect.IsEnabled)
        {
            return;
        }

        if (isPressed && (touchEffect.CurrentHoverStatus is HoverStatus.Entered))
        {
            touchEffect.HandleTouch(TouchStatus.Completed);
            AnimateTilt(pointerUpStoryboard);
        }
        else if (touchEffect.CurrentHoverStatus is not HoverStatus.Exited)
        {
            touchEffect.HandleTouch(TouchStatus.Cancelled);
            AnimateTilt(pointerUpStoryboard);
        }

        touchEffect.HandleUserInteraction(TouchInteractionStatus.Completed);
        e.Handled = true;

        isPressed = false;
        isIntentionalCaptureLoss = true;
    }

    void OnPointerPressed(object? sender, PointerRoutedEventArgs e)
    {
        if (touchEffect is null || touchEffect.Element is null || !touchEffect.IsEnabled || sender is not FrameworkElement container)
        {
            return;
        }

        isPressed = true;
        container.CapturePointer(e.Pointer);

        touchEffect.HandleUserInteraction(TouchInteractionStatus.Started);
        touchEffect.HandleTouch(TouchStatus.Started);

        AnimateTilt(pointerDownStoryboard);
        isIntentionalCaptureLoss = false;
        e.Handled = true;
    }

    void AnimateTilt(Storyboard? storyboard)
    {
        if (storyboard != null && touchEffect?.Element != null && touchEffect.NativeAnimation)
        {
            try
            {
                storyboard.Stop();
                storyboard.Begin();
            }
            catch
            {
                // Suppress
            }
        }
    }
}
