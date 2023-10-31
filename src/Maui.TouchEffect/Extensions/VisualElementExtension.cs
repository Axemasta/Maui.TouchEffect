namespace Maui.TouchEffect.Extensions;

/// <summary>
/// Extension methods for <see cref="VisualElement" />.
/// </summary>
public static class VisualElementExtension
{
	public static Task<bool> ColorTo(this VisualElement element, Color color, uint length = 250u, Easing easing = null)
	{
		_ = element ?? throw new ArgumentNullException(nameof(element));

		var animationCompletionSource = new TaskCompletionSource<bool>();

        var backgroundColor = element.BackgroundColor ?? Colors.Transparent;

		new Animation
		{
			{
				0, 1, new Animation(v => backgroundColor = new Color(Convert.ToSingle(v), backgroundColor.Green, backgroundColor.Blue, backgroundColor.Alpha), backgroundColor.Red, color.Red)
			},
			{
				0, 1, new Animation(v => backgroundColor = new Color(backgroundColor.Red, Convert.ToSingle(v), backgroundColor.Blue, backgroundColor.Alpha), backgroundColor.Green, color.Green)
			},
			{
				0, 1, new Animation(v => backgroundColor = new Color(backgroundColor.Red, backgroundColor.Green, Convert.ToSingle(v), backgroundColor.Alpha), backgroundColor.Blue, color.Blue)
			},
			{
				0, 1, new Animation(v => backgroundColor = new Color(backgroundColor.Red, backgroundColor.Green, backgroundColor.Blue, Convert.ToSingle(v)), backgroundColor.Alpha, color.Alpha)
			},
		}.Commit(element, nameof(ColorTo), 16, length, easing, (d, b) => animationCompletionSource.SetResult(true));

		return animationCompletionSource.Task;
	}

	public static void AbortAnimations(this VisualElement element, params string[] otherAnimationNames)
	{
		_ = element ?? throw new ArgumentNullException(nameof(element));

		element.CancelAnimations();
		_ = element.AbortAnimation(nameof(ColorTo));

		if (otherAnimationNames == null)
		{
			return;
		}

		foreach (var name in otherAnimationNames)
		{
			_ = element.AbortAnimation(name);
		}
	}

	internal static bool TryFindParentElementWithParentOfType<T>(this VisualElement element, out VisualElement result, out T parent) where T : VisualElement
	{
		result = null;
		parent = null;

		while (element?.Parent != null)
		{
			if (element.Parent is not T parentElement)
			{
				element = element.Parent as VisualElement;
				continue;
			}

			result = element;
			parent = parentElement;

			return true;
		}

		return false;
	}

	internal static bool TryFindParentOfType<T>(this VisualElement element, out T parent) where T : VisualElement
	{
		return element.TryFindParentElementWithParentOfType(out _, out parent);
	}
}
