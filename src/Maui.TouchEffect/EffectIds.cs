namespace Maui.TouchEffect;

internal sealed class EffectIds
{
	/// <summary>
	/// The Base Resolution Group Name For Effects
	/// </summary>
	private readonly static string EffectResolutionGroupName = $"{nameof(MauiTouchEffect)}.{nameof(Enums)}";

	/// <summary>
	/// Effect Id for <see cref="TouchEffect" />
	/// </summary>
	public static string TouchEffect => $"{EffectResolutionGroupName}.{nameof(TouchEffect)}";
}
