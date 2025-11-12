using Maui.TouchEffect.Enums;
using Maui.TouchEffect.Extensions;

namespace Maui.TouchEffect;
public partial class TouchEffect : IDisposable
{
    bool isDisposed;

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    internal void RaiseGestureCompleted()
    {
        var cElement = Element;

        if (cElement is null)
        {
            return;
        }

        var parameter = CommandParameter;

        if (Command?.CanExecute(parameter) is true)
        {
            Command.Execute(parameter);
        }

        weakEventManager.HandleEvent(cElement, new TouchCompletedEventArgs(parameter), nameof(TouchGestureCompleted));
    }

    internal void RaiseLongPressCompleted()
    {
        var cElement = Element;

        if (cElement is null)
        {
            return;
        }

        var parameter = LongPressCommandParameter ?? CommandParameter;

        if (LongPressCommand?.CanExecute(parameter) is true)
        {
            LongPressCommand.Execute(parameter);
        }

        weakEventManager.HandleEvent(cElement, new LongPressCompletedEventArgs(parameter), nameof(LongPressCompleted));
    }

    internal void ForceUpdateState(bool animated = true)
    {
        if (Element == null)
        {
            return;
        }

        gestureManager.ChangeStateAsync(this, animated).SafeFireAndForget();
    }

    internal void HandleTouch(TouchStatus status)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);

        gestureManager.HandleTouch(this, status);
    }

    internal void HandleUserInteraction(TouchInteractionStatus interactionStatus)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);

        GestureManager.HandleUserInteraction(this, interactionStatus);
    }

    internal void HandleHover(HoverStatus status)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);

        GestureManager.HandleHover(this, status);
    }

    /// <summary>
	/// Dispose the object.
	/// </summary>
	protected virtual void Dispose(bool disposing)
    {
        if (isDisposed)
        {
            return;
        }

        if (disposing)
        {
            // free managed resources
            gestureManager.Dispose();
            //PlatformDispose();
        }

        isDisposed = true;
    }

    internal void HandleLongPress()
    {
        if (Element is null)
        {
            return;
        }

        gestureManager.HandleLongPress(this);
    }

    void SetChildrenInputTransparent(bool shouldSetTransparent)
    {
        switch (Element)
        {
            case Layout layout:
                SetChildrenInputTransparent(shouldSetTransparent, layout);
                return;
            case IContentView { Content: Layout contentLayout }:
                SetChildrenInputTransparent(shouldSetTransparent, contentLayout);
                break;
        }
    }

    void SetChildrenInputTransparent(bool shouldSetTransparent, Layout layout)
    {
        layout.ChildAdded -= OnLayoutChildAdded;

        if (!shouldSetTransparent)
        {
            return;
        }

        layout.InputTransparent = false;
        foreach (var view in layout.Children)
        {
            OnLayoutChildAdded(layout, new ElementEventArgs((View)view));
        }

        layout.ChildAdded += OnLayoutChildAdded;
    }

    void OnLayoutChildAdded(object? sender, ElementEventArgs e)
    {
        if (e.Element is not View view)
        {
            return;
        }

        if (!ShouldMakeChildrenInputTransparent)
        {
            view.InputTransparent = false;
            return;
        }

        view.InputTransparent = IsEnabled;
    }

    internal void RaiseCurrentTouchStateChanged()
    {
        ForceUpdateState();
        HandleLongPress();

        if (Element is null)
        {
            return;
        }

        weakEventManager.HandleEvent(Element, new TouchStateChangedEventArgs(CurrentTouchState), nameof(CurrentTouchStateChanged));
    }

    internal void RaiseInteractionStatusChanged()
    {
        if (Element is null)
        {
            return;
        }

        weakEventManager.HandleEvent(Element, new TouchInteractionStatusChangedEventArgs(CurrentInteractionStatus), nameof(InteractionStatusChanged));
    }

    internal void RaiseCurrentTouchStatusChanged()
    {
        if (Element is null)
        {
            return;
        }

        weakEventManager.HandleEvent(Element, new TouchStatusChangedEventArgs(CurrentTouchStatus), nameof(CurrentTouchStatusChanged));
    }

    internal void RaiseHoverStateChanged()
    {
        ForceUpdateState();

        if (Element is null)
        {
            return;
        }

        weakEventManager.HandleEvent(Element, new HoverStateChangedEventArgs(CurrentHoverState), nameof(HoverStateChanged));
    }

    internal void RaiseHoverStatusChanged()
    {
        if (Element is null)
        {
            return;
        }

        weakEventManager.HandleEvent(Element, new HoverStatusChangedEventArgs(CurrentHoverStatus), nameof(HoverStatusChanged));
    }

    internal static TouchEffect? GetFrom(BindableObject? bindable)
    {
        var effects = (bindable as VisualElement)?.Effects?.OfType<TouchEffect>().ToList();
        return effects?.FirstOrDefault(x => !x.IsAutoGenerated) ?? effects?.FirstOrDefault();
    }

    internal static TouchEffect? PickFrom(BindableObject? bindable)
    {
        var effects = (bindable as VisualElement)?.Effects?.OfType<TouchEffect>().ToList();
        return effects?.FirstOrDefault(x => !x.IsAutoGenerated && !x.IsUsed)
                ?? effects?.FirstOrDefault(x => x.IsAutoGenerated)
                ?? effects?.FirstOrDefault();
    }

    protected static void TryGenerateEffect(BindableObject? bindable, object oldValue, object newValue)
    {
        if (bindable is not VisualElement view || view.Effects.OfType<TouchEffect>().Any())
        {
            return;
        }

        view.Effects.Add(new TouchEffect
        {
            IsAutoGenerated = true,
        });
    }

    protected static void ForceUpdateStateAndTryGenerateEffect(BindableObject bindable, object oldValue, object newValue)
    {
        GetFrom(bindable)?.ForceUpdateState();
        TryGenerateEffect(bindable, oldValue, newValue);
    }

    private static void ForceUpdateStateWithoutAnimationAndTryGenerateEffect(BindableObject bindable, object oldValue, object newValue)
    {
        GetFrom(bindable)?.ForceUpdateState();
        TryGenerateEffect(bindable, oldValue, newValue);
    }

    private static void SetChildrenInputTransparentAndTryGenerateEffect(BindableObject bindable, object oldValue, object newValue)
    {
        GetFrom(bindable)?.SetChildrenInputTransparent((bool)newValue);
        TryGenerateEffect(bindable, oldValue, newValue);
    }
}
