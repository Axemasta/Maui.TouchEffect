using Maui.TouchEffect.Enums;
using Maui.TouchEffect.Extensions;
using static System.Math;

namespace Maui.TouchEffect;

internal sealed class GestureManager
{
	private const int _animationProgressDelay = 10;
	private Color? _defaultBackgroundColor;
	private CancellationTokenSource? _longPressTokenSource;
	private CancellationTokenSource? _animationTokenSource;
	private Func<TouchEffect, TouchState, HoverState, int, Easing, CancellationToken, Task>? _animationTaskFactory;
	private double? _durationMultiplier;
	private double _animationProgress;
	private TouchState? _animationState;

	internal void HandleTouch(TouchEffect sender, TouchStatus status)
	{
		if (sender.IsDisabled)
		{
			return;
		}

		var canExecuteAction = sender.CanExecute;
		if (status != TouchStatus.Started || canExecuteAction)
		{
			if (!canExecuteAction)
			{
				status = TouchStatus.Canceled;
			}

			var state = status == TouchStatus.Started
				? TouchState.Pressed
				: TouchState.Normal;

			if (status == TouchStatus.Started)
			{
				_animationProgress = 0;
				_animationState = state;
			}

			var isToggled = sender.IsToggled;
			if (isToggled.HasValue)
			{
				if (status != TouchStatus.Started)
				{
					_durationMultiplier = _animationState == TouchState.Pressed && !isToggled.Value ||
										_animationState == TouchState.Normal && isToggled.Value
						? 1 - _animationProgress
						: _animationProgress;

					UpdateStatusAndState(sender, status, state);

					if (status == TouchStatus.Canceled)
					{
						sender.ForceUpdateState(false);
						return;
					}

					OnTapped(sender);
					sender.IsToggled = !isToggled;
					return;
				}

				state = isToggled.Value
					? TouchState.Normal
					: TouchState.Pressed;
			}

			UpdateStatusAndState(sender, status, state);
		}

		if (status == TouchStatus.Completed)
		{
			OnTapped(sender);
		}
	}

	internal static void HandleUserInteraction(TouchEffect sender, TouchInteractionStatus interactionStatus)
	{
		if (sender.InteractionStatus != interactionStatus)
		{
			sender.InteractionStatus = interactionStatus;
			sender.RaiseInteractionStatusChanged();
		}
	}

	internal static void HandleHover(TouchEffect sender, HoverStatus status)
	{
		if (!sender.Element?.IsEnabled ?? true)
		{
			return;
		}

		var hoverState = status == HoverStatus.Entered
			? HoverState.Hovered
			: HoverState.Normal;

		if (sender.HoverState != hoverState)
		{
			sender.HoverState = hoverState;
			sender.RaiseHoverStateChanged();
		}

		if (sender.HoverStatus != status)
		{
			sender.HoverStatus = status;
			sender.RaiseHoverStatusChanged();
		}
	}

	internal async Task ChangeStateAsync(TouchEffect sender, bool animated)
	{
		var status = sender.Status;
		var state = sender.State;
		var hoverState = sender.HoverState;

		AbortAnimations(sender);
		_animationTokenSource = new CancellationTokenSource();
		var token = _animationTokenSource.Token;

		var isToggled = sender.IsToggled;

		if (sender.Element != null)
		{
			UpdateVisualState(sender.Element, state, hoverState);
		}

		if (!animated)
		{
			if (isToggled.HasValue)
			{
				state = isToggled.Value
					? TouchState.Pressed
					: TouchState.Normal;
			}

			var durationMultiplier = _durationMultiplier;
			_durationMultiplier = null;

			await RunAnimationTask(sender, state, hoverState, _animationTokenSource.Token, durationMultiplier.GetValueOrDefault()).ConfigureAwait(false);
			return;
		}

		var pulseCount = sender.PulseCount;

		if (pulseCount == 0 || state == TouchState.Normal && !isToggled.HasValue)
		{
			if (isToggled.HasValue)
			{
				state =
					status == TouchStatus.Started && isToggled.Value ||
					status != TouchStatus.Started && !isToggled.Value
						? TouchState.Normal
						: TouchState.Pressed;
			}

			await RunAnimationTask(sender, state, hoverState, _animationTokenSource.Token).ConfigureAwait(false);
			return;
		}

		do
		{
			var rippleState = isToggled.HasValue && isToggled.Value
				? TouchState.Normal
				: TouchState.Pressed;

			await RunAnimationTask(sender, rippleState, hoverState, _animationTokenSource.Token);
			if (token.IsCancellationRequested)
			{
				return;
			}

			rippleState = isToggled.HasValue && isToggled.Value
				? TouchState.Pressed
				: TouchState.Normal;

			await RunAnimationTask(sender, rippleState, hoverState, _animationTokenSource.Token);
			if (token.IsCancellationRequested)
			{
				return;
			}
		} while (--pulseCount != 0);
	}

	internal void HandleLongPress(TouchEffect sender)
	{
		if (sender.State == TouchState.Normal)
		{
			_longPressTokenSource?.Cancel();
			_longPressTokenSource?.Dispose();
			_longPressTokenSource = null;
			return;
		}

		if (sender.LongPressCommand == null || sender.InteractionStatus == TouchInteractionStatus.Completed)
		{
			return;
		}

		_longPressTokenSource = new CancellationTokenSource();
		_ = Task.Delay(sender.LongPressDuration, _longPressTokenSource.Token).ContinueWith(t =>
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
				sender.HandleUserInteraction(TouchInteractionStatus.Completed);
				sender.RaiseLongPressCompleted();
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

	internal void SetCustomAnimationTask(Func<TouchEffect, TouchState, HoverState, int, Easing, CancellationToken, Task>? animationTaskFactory)
	{
		_animationTaskFactory = animationTaskFactory;
	}

	internal void Reset()
	{
		SetCustomAnimationTask(null);
		_defaultBackgroundColor = default;
	}

	internal void OnTapped(TouchEffect sender)
	{
		if (!sender.CanExecute || sender.LongPressCommand != null && sender.InteractionStatus == TouchInteractionStatus.Completed)
		{
			return;
		}

		if (DeviceInfo.Current.Platform == DevicePlatform.Android)
		{
			HandleCollectionViewSelection(sender);
		}

		if (sender.Element is IButtonController button)
		{
			button.SendClicked();
		}

		sender.RaiseCompleted();
	}

	private static void HandleCollectionViewSelection(TouchEffect sender)
	{
		if (!sender.Element.TryFindParentElementWithParentOfType(out var result, out CollectionView parent))
		{
			return;
		}

		var collectionView = parent ?? throw new NullReferenceException();
		var item = result?.BindingContext ?? result ?? throw new NullReferenceException();

		switch (collectionView.SelectionMode)
		{
			case SelectionMode.Single:
				collectionView.SelectedItem = item;
				break;
			case SelectionMode.Multiple:
				var selectedItems = collectionView.SelectedItems?.ToList() ?? new List<object>();

				if (selectedItems.Contains(item))
				{
					_ = selectedItems.Remove(item);
				}
				else
				{
					selectedItems.Add(item);
				}

				collectionView.UpdateSelectedItems(selectedItems);
				break;
		}
	}

	internal void AbortAnimations(TouchEffect sender)
	{
		_animationTokenSource?.Cancel();
		_animationTokenSource?.Dispose();
		_animationTokenSource = null;
		var element = sender.Element;
		if (element == null)
		{
			return;
		}

		element.AbortAnimations();
	}

	private static void UpdateStatusAndState(TouchEffect sender, TouchStatus status, TouchState state)
	{
		sender.Status = status;
		sender.RaiseStatusChanged();

		if (sender.State != state || status != TouchStatus.Canceled)
		{
			sender.State = state;
			sender.RaiseStateChanged();
		}
	}

	private static void UpdateVisualState(VisualElement visualElement, TouchState touchState, HoverState hoverState)
	{
		var state = touchState == TouchState.Pressed
			? TouchEffect.PressedVisualState
			: hoverState == HoverState.Hovered
				? TouchEffect.HoveredVisualState
				: TouchEffect.UnpressedVisualState;

		_ = VisualStateManager.GoToState(visualElement, state);
	}

	private static async Task SetBackgroundImageAsync(TouchEffect sender, TouchState touchState, HoverState hoverState, int duration, CancellationToken token)
	{
		var normalBackgroundImageSource = sender.NormalBackgroundImageSource;
		var pressedBackgroundImageSource = sender.PressedBackgroundImageSource;
		var hoveredBackgroundImageSource = sender.HoveredBackgroundImageSource;

		if (normalBackgroundImageSource == null &&
			pressedBackgroundImageSource == null &&
			hoveredBackgroundImageSource == null)
		{
			return;
		}

		var aspect = sender.BackgroundImageAspect;
		var source = normalBackgroundImageSource;
		if (touchState == TouchState.Pressed)
		{
			if (sender.Element?.IsSet(TouchEffect.PressedBackgroundImageAspectProperty) ?? false)
			{
				aspect = sender.PressedBackgroundImageAspect;
			}

			source = pressedBackgroundImageSource;
		}
		else if (hoverState == HoverState.Hovered)
		{
			if (sender.Element?.IsSet(TouchEffect.HoveredBackgroundImageAspectProperty) ?? false)
			{
				aspect = sender.HoveredBackgroundImageAspect;
			}

			if (sender.Element?.IsSet(TouchEffect.HoveredBackgroundImageSourceProperty) ?? false)
			{
				source = hoveredBackgroundImageSource;
			}
		}
		else
		{
			if (sender.Element?.IsSet(TouchEffect.NormalBackgroundImageAspectProperty) ?? false)
			{
				aspect = sender.NormalBackgroundImageAspect;
			}
		}

		try
		{
			if (sender.ShouldSetImageOnAnimationEnd && duration > 0)
			{
				await Task.Delay(duration, token);
			}
		}
		catch (TaskCanceledException)
		{
			return;
		}

		if (sender.Element is Image image)
		{
			using (image.Batch())
			{
				image.Aspect = aspect;
				image.Source = source;
			}
		}
	}

	private Task SetBackgroundColor(TouchEffect sender, TouchState touchState, HoverState hoverState, int duration, Easing easing)
	{
		var normalBackgroundColor = sender.NormalBackgroundColor;
		var pressedBackgroundColor = sender.PressedBackgroundColor;
		var hoveredBackgroundColor = sender.HoveredBackgroundColor;

		if (sender.Element == null
			|| normalBackgroundColor is null
			&& pressedBackgroundColor is null
			&& hoveredBackgroundColor is null)
		{
			return Task.FromResult(false);
		}

		var element = sender.Element;
		if (_defaultBackgroundColor == default)
		{
			_defaultBackgroundColor = element.BackgroundColor ?? normalBackgroundColor;
		}

		var color = GetBackgroundColor(normalBackgroundColor);

		if (touchState == TouchState.Pressed)
		{
			color = GetBackgroundColor(pressedBackgroundColor);
		}
		else if (hoverState == HoverState.Hovered && sender.Element.IsSet(TouchEffect.HoveredBackgroundColorProperty))
		{
			color = GetBackgroundColor(hoveredBackgroundColor);
		}

		if (duration <= 0)
		{
			element.AbortAnimations();
			element.BackgroundColor = color;
			return Task.FromResult(true);
		}

		return element.ColorTo(color, (uint)duration, easing);
	}

	private static Task? SetOpacity(TouchEffect sender, TouchState touchState, HoverState hoverState, int duration, Easing easing)
	{
		var normalOpacity = sender.NormalOpacity;
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
		var normalScale = sender.NormalScale;
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
		var normalTranslationX = sender.NormalTranslationX;
		var pressedTranslationX = sender.PressedTranslationX;
		var hoveredTranslationX = sender.HoveredTranslationX;

		var normalTranslationY = sender.NormalTranslationY;
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
		var normalRotation = sender.NormalRotation;
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
		var normalRotationX = sender.NormalRotationX;
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
		var normalRotationY = sender.NormalRotationY;
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

	private Color? GetBackgroundColor(Color? color)
	{
		return color != KnownColor.Default
			? color
			: _defaultBackgroundColor;
	}

	private Task RunAnimationTask(TouchEffect sender, TouchState touchState, HoverState hoverState, CancellationToken token, double? durationMultiplier = null)
	{
		if (sender.Element == null)
		{
			return Task.FromResult(false);
		}

		var duration = sender.AnimationDuration;
		var easing = sender.AnimationEasing;

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
			if (sender.Element.IsSet(TouchEffect.NormalAnimationDurationProperty))
			{
				duration = sender.NormalAnimationDuration;
			}

			if (sender.Element.IsSet(TouchEffect.NormalAnimationEasingProperty))
			{
				easing = sender.NormalAnimationEasing;
			}
		}

		if (durationMultiplier.HasValue)
		{
			duration = (int)durationMultiplier.Value * duration;
		}

		duration = Max(duration, 0);

		return Task.WhenAll(
			_animationTaskFactory?.Invoke(sender, touchState, hoverState, duration, easing, token) ?? Task.FromResult(true),
			SetBackgroundImageAsync(sender, touchState, hoverState, duration, token),
			SetBackgroundColor(sender, touchState, hoverState, duration, easing),
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
}
