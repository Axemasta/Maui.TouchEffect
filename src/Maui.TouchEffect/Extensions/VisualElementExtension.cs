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

		new Animation
		{
			{
				0, 1, new Animation(v => element.BackgroundColor = new Color(Convert.ToSingle(v), element.BackgroundColor.Green, element.BackgroundColor.Blue, element.BackgroundColor.Alpha), element.BackgroundColor.Red, color.Red)
			},
			{
				0, 1, new Animation(v => element.BackgroundColor = new Color(element.BackgroundColor.Red, Convert.ToSingle(v), element.BackgroundColor.Blue, element.BackgroundColor.Alpha), element.BackgroundColor.Green, color.Green)
			},
			{
				0, 1, new Animation(v => element.BackgroundColor = new Color(element.BackgroundColor.Red, element.BackgroundColor.Green, Convert.ToSingle(v), element.BackgroundColor.Alpha), element.BackgroundColor.Blue, color.Blue)
			},
			{
				0, 1, new Animation(v => element.BackgroundColor = new Color(element.BackgroundColor.Red, element.BackgroundColor.Green, element.BackgroundColor.Blue, Convert.ToSingle(v)), element.BackgroundColor.Alpha, color.Alpha)
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
