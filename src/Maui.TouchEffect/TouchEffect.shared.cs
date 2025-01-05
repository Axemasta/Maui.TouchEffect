using System.ComponentModel;
using System.Windows.Input;
using Maui.TouchEffect.Converters;
using Maui.TouchEffect.Enums;
// ReSharper disable MemberCanBePrivate.Global
namespace Maui.TouchEffect;

public partial class TouchEffect : RoutingEffect
{
    /// <summary>
    /// The visual state for when the <see cref="TouchState"/> is <see cref="TouchState.Default"/>.
    /// </summary>
    public const string UnpressedVisualState = "Unpressed";

    /// <summary>
    /// The visual state for when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
    /// </summary>
    public const string PressedVisualState = "Pressed";

    /// <summary>
    /// The visual state for when the <see cref="CurrentHoverState"/> is <see cref="HoverState.Hovered"/>.
    /// </summary>
    public const string HoveredVisualState = "Hovered";

    #region Bindable Properties

    /// <summary>
    /// Bindable property for <see cref="IsEnabled"/>
    /// </summary>
    public readonly static BindableProperty IsEnabledProperty = BindableProperty.CreateAttached(
		nameof(IsEnabled),
		typeof(bool),
		typeof(TouchEffect),
		true,
		propertyChanged: TryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="Command"/>
	/// </summary>
    public readonly static BindableProperty CommandProperty = BindableProperty.CreateAttached(
        nameof(Command),
        typeof(ICommand),
        typeof(TouchEffect),
        default(ICommand),
        propertyChanged: TryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="ShouldMakeChildrenInputTransparent"/>
	/// </summary>
    public readonly static BindableProperty ShouldMakeChildrenInputTransparentProperty = BindableProperty.CreateAttached(
		nameof(ShouldMakeChildrenInputTransparent),
		typeof(bool),
		typeof(TouchEffect),
		true,
		propertyChanged: SetChildrenInputTransparentAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="DisallowTouchThreshold"/>
	/// </summary>
    public readonly static BindableProperty DisallowTouchThresholdProperty = BindableProperty.CreateAttached(
        nameof(DisallowTouchThreshold),
        typeof(int),
        typeof(TouchEffect),
        default(int),
        propertyChanged: TryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="LongPressCommand"/>
	/// </summary>
    public readonly static BindableProperty LongPressCommandProperty = BindableProperty.CreateAttached(
		nameof(LongPressCommand),
		typeof(ICommand),
		typeof(TouchEffect),
		default(ICommand),
		propertyChanged: TryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="CurrentTouchStatus"/>
	/// </summary>
    public readonly static BindableProperty CurrentTouchStatusProperty = BindableProperty.CreateAttached(
        nameof(CurrentTouchStatus),
        typeof(TouchStatus),
        typeof(TouchEffect),
        TouchStatus.Completed,
        BindingMode.OneWayToSource);

    /// <summary>
	/// Bindable property for <see cref="LongPressDuration"/>
	/// </summary>
    public readonly static BindableProperty LongPressDurationProperty = BindableProperty.CreateAttached(
        nameof(LongPressDuration),
        typeof(TimeSpan),
        typeof(TouchEffect),
        TimeSpan.FromSeconds(0.5),
        propertyChanged: TryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="LongPressCommandParameter"/>
	/// </summary>
    public readonly static BindableProperty LongPressCommandParameterProperty = BindableProperty.CreateAttached(
        nameof(LongPressCommandParameter),
        typeof(object),
        typeof(TouchEffect),
        default,
        propertyChanged: TryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="CurrentHoverStatus"/>
	/// </summary>
    public readonly static BindableProperty CurrentHoverStatusProperty = BindableProperty.CreateAttached(
        nameof(CurrentHoverStatus),
        typeof(HoverStatus),
        typeof(TouchEffect),
        HoverStatus.Exited,
        BindingMode.OneWayToSource);

    /// <summary>
	/// Bindable property for <see cref="CurrentInteractionStatus"/>
	/// </summary>
    public readonly static BindableProperty CurrentInteractionStatusProperty = BindableProperty.CreateAttached(
        nameof(CurrentInteractionStatus),
        typeof(TouchInteractionStatus),
        typeof(TouchEffect),
        TouchInteractionStatus.Completed,
        BindingMode.OneWayToSource);

    /// <summary>
	/// Bindable property for <see cref="CurrentTouchState"/>
	/// </summary>
    public readonly static BindableProperty CurrentTouchStateProperty = BindableProperty.CreateAttached(
		nameof(CurrentTouchState),
		typeof(TouchState),
		typeof(TouchEffect),
		TouchState.Default,
		BindingMode.OneWayToSource);

    /// <summary>
	/// Bindable property for <see cref="DefaultBackgroundColor"/>
	/// </summary>
    public readonly static BindableProperty DefaultBackgroundColorProperty = BindableProperty.CreateAttached(
        nameof(DefaultBackgroundColor),
        typeof(Color),
        typeof(TouchEffect),
        KnownColor.Default,
        propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="CurrentHoverState"/>
	/// </summary>
    public readonly static BindableProperty CurrentHoverStateProperty = BindableProperty.CreateAttached(
        nameof(CurrentHoverState),
        typeof(HoverState),
        typeof(TouchEffect),
        HoverState.Default,
        BindingMode.OneWayToSource);

    /// <summary>
	/// Bindable property for <see cref="CommandParameter"/>
	/// </summary>
    public readonly static BindableProperty CommandParameterProperty = BindableProperty.CreateAttached(
        nameof(CommandParameter),
        typeof(object),
        typeof(TouchEffect),
        default,
        propertyChanged: TryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="DefaultScale"/>
	/// </summary>
    public readonly static BindableProperty DefaultScaleProperty = BindableProperty.CreateAttached(
        nameof(DefaultScale),
        typeof(double),
        typeof(TouchEffect),
        1.0,
        propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="PressedOpacity"/>
	/// </summary>
    public readonly static BindableProperty PressedOpacityProperty = BindableProperty.CreateAttached(
        nameof(PressedOpacity),
        typeof(double),
        typeof(TouchEffect),
        1.0,
        propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="HoveredOpacity"/>
	/// </summary>
    public readonly static BindableProperty HoveredOpacityProperty = BindableProperty.CreateAttached(
        nameof(HoveredOpacity),
        typeof(double),
        typeof(TouchEffect),
        1.0,
        propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="DefaultOpacity"/>
	/// </summary>
    public readonly static BindableProperty DefaultOpacityProperty = BindableProperty.CreateAttached(
        nameof(DefaultOpacity),
        typeof(double),
        typeof(TouchEffect),
        1.0,
        propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="PressedScale"/>
	/// </summary>
    public readonly static BindableProperty PressedScaleProperty = BindableProperty.CreateAttached(
        nameof(PressedScale),
        typeof(double),
        typeof(TouchEffect),
        1.0,
        propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="HoveredScale"/>
	/// </summary>
    public readonly static BindableProperty HoveredScaleProperty = BindableProperty.CreateAttached(
            nameof(HoveredScale),
            typeof(double),
            typeof(TouchEffect),
            1.0,
            propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="PressedBackgroundColor"/>
	/// </summary>
	public readonly static BindableProperty PressedBackgroundColorProperty = BindableProperty.CreateAttached(
        nameof(PressedBackgroundColor),
        typeof(Color),
        typeof(TouchEffect),
        KnownColor.Default,
        propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="HoveredBackgroundColor"/>
	/// </summary>
    public readonly static BindableProperty HoveredBackgroundColorProperty = BindableProperty.CreateAttached(
		nameof(HoveredBackgroundColor),
		typeof(Color),
		typeof(TouchEffect),
        KnownColor.Default,
		propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="PressedTranslationX"/>
	/// </summary>
    public readonly static BindableProperty PressedTranslationXProperty = BindableProperty.CreateAttached(
		nameof(PressedTranslationX),
		typeof(double),
		typeof(TouchEffect),
		0.0,
		propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="HoveredTranslationX"/>
	/// </summary>
    public readonly static BindableProperty HoveredTranslationXProperty = BindableProperty.CreateAttached(
        nameof(HoveredTranslationX),
        typeof(double),
        typeof(TouchEffect),
        0.0,
        propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="HoveredTranslationY"/>
	/// </summary>
    public readonly static BindableProperty HoveredTranslationYProperty = BindableProperty.CreateAttached(
        nameof(HoveredTranslationY),
        typeof(double),
        typeof(TouchEffect),
        0.0,
        propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="DefaultTranslationX"/>
	/// </summary>
    public readonly static BindableProperty DefaultTranslationXProperty = BindableProperty.CreateAttached(
		nameof(DefaultTranslationX),
		typeof(double),
		typeof(TouchEffect),
		0.0,
		propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="PressedRotationX"/>
	/// </summary>
    public readonly static BindableProperty PressedRotationXProperty = BindableProperty.CreateAttached(
        nameof(PressedRotationX),
        typeof(double),
        typeof(TouchEffect),
        0.0,
        propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="HoveredRotationX"/>
	/// </summary>
    public readonly static BindableProperty HoveredRotationXProperty = BindableProperty.CreateAttached(
        nameof(HoveredRotationX),
        typeof(double),
        typeof(TouchEffect),
        0.0,
        propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="DefaultRotationX"/>
	/// </summary>
    public readonly static BindableProperty DefaultRotationXProperty = BindableProperty.CreateAttached(
        nameof(DefaultRotationX),
        typeof(double),
        typeof(TouchEffect),
        0.0,
        propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="PressedRotation"/>
	/// </summary>
    public readonly static BindableProperty PressedRotationProperty = BindableProperty.CreateAttached(
        nameof(PressedRotation),
        typeof(double),
        typeof(TouchEffect),
        0.0,
        propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="PressedRotationY"/>
	/// </summary>
    public readonly static BindableProperty PressedRotationYProperty = BindableProperty.CreateAttached(
        nameof(PressedRotationY),
        typeof(double),
        typeof(TouchEffect),
        0.0,
        propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="DefaultRotationY"/>
	/// </summary>
    public readonly static BindableProperty DefaultRotationYProperty = BindableProperty.CreateAttached(
        nameof(DefaultRotationY),
        typeof(double),
        typeof(TouchEffect),
        0.0,
        propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="HoveredRotation"/>
	/// </summary>
    public readonly static BindableProperty HoveredRotationProperty = BindableProperty.CreateAttached(
        nameof(HoveredRotation),
        typeof(double),
        typeof(TouchEffect),
        0.0,
        propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="DefaultRotation"/>
	/// </summary>
    public readonly static BindableProperty DefaultRotationProperty = BindableProperty.CreateAttached(
        nameof(DefaultRotation),
        typeof(double),
        typeof(TouchEffect),
        0.0,
        propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="PressedTranslationY"/>
	/// </summary>
    public readonly static BindableProperty PressedTranslationYProperty = BindableProperty.CreateAttached(
        nameof(PressedTranslationY),
        typeof(double),
        typeof(TouchEffect),
        0.0,
        propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="HoveredAnimationEasing"/>
	/// </summary>
    public readonly static BindableProperty HoveredAnimationEasingProperty = BindableProperty.CreateAttached(
        nameof(HoveredAnimationEasing),
        typeof(Easing),
        typeof(TouchEffect),
        null,
        propertyChanged: TryGenerateEffect);

    /// <summary>
    /// Bindable property for <see cref="HoveredRotationY"/>
    /// </summary>
    public readonly static BindableProperty HoveredRotationYProperty = BindableProperty.CreateAttached(
        nameof(HoveredRotationY),
        typeof(double),
        typeof(TouchEffect),
        0.0,
        propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="DefaultTranslationY"/>
	/// </summary>
    public readonly static BindableProperty DefaultTranslationYProperty = BindableProperty.CreateAttached(
		nameof(DefaultTranslationY),
		typeof(double),
		typeof(TouchEffect),
		0.0,
		propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="PressedAnimationDuration"/>
	/// </summary>
    public readonly static BindableProperty PressedAnimationDurationProperty = BindableProperty.CreateAttached(
        nameof(PressedAnimationDuration),
        typeof(int),
        typeof(TouchEffect),
        default(int),
        propertyChanged: TryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="HoveredAnimationDuration"/>
	/// </summary>
    public readonly static BindableProperty HoveredAnimationDurationProperty = BindableProperty.CreateAttached(
        nameof(HoveredAnimationDuration),
        typeof(int),
        typeof(TouchEffect),
        default(int),
        propertyChanged: TryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="DefaultAnimationEasing"/>
	/// </summary>
    public readonly static BindableProperty DefaultAnimationEasingProperty = BindableProperty.CreateAttached(
        nameof(DefaultAnimationEasing),
        typeof(Easing),
        typeof(TouchEffect),
        null,
        propertyChanged: TryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="DefaultAnimationDuration"/>
	/// </summary>
    public readonly static BindableProperty DefaultAnimationDurationProperty = BindableProperty.CreateAttached(
        nameof(DefaultAnimationDuration),
        typeof(int),
        typeof(TouchEffect),
        default(int),
        propertyChanged: TryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="PressedAnimationEasing"/>
	/// </summary>
    public readonly static BindableProperty PressedAnimationEasingProperty = BindableProperty.CreateAttached(
        nameof(PressedAnimationEasing),
        typeof(Easing),
        typeof(TouchEffect),
        null,
        propertyChanged: TryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="DefaultImageSource"/>
	/// </summary>
    public readonly static BindableProperty DefaultImageSourceProperty = BindableProperty.CreateAttached(
        nameof(DefaultImageSource),
        typeof(ImageSource),
        typeof(TouchEffect),
        default(ImageSource),
        propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="HoveredImageSource"/>
	/// </summary>
    public readonly static BindableProperty HoveredImageSourceProperty = BindableProperty.CreateAttached(
        nameof(HoveredImageSource),
        typeof(ImageSource),
        typeof(TouchEffect),
        default(ImageSource),
        propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="PressedImageSource"/>
	/// </summary>
    public readonly static BindableProperty PressedImageSourceProperty = BindableProperty.CreateAttached(
        nameof(PressedImageSource),
        typeof(ImageSource),
        typeof(TouchEffect),
        default(ImageSource),
        propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="DefaultImageAspect"/>
	/// </summary>
    public readonly static BindableProperty DefaultImageAspectProperty = BindableProperty.CreateAttached(
        nameof(DefaultImageAspect),
        typeof(Aspect),
        typeof(TouchEffect),
        default(Aspect),
        propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="HoveredImageAspect"/>
	/// </summary>
    public readonly static BindableProperty HoveredImageAspectProperty = BindableProperty.CreateAttached(
        nameof(HoveredImageAspect),
        typeof(Aspect),
        typeof(TouchEffect),
        default(Aspect),
        propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="PressedImageAspect"/>
	/// </summary>
    public readonly static BindableProperty PressedImageAspectProperty = BindableProperty.CreateAttached(
        nameof(PressedImageAspect),
        typeof(Aspect),
        typeof(TouchEffect),
        default(Aspect),
        propertyChanged: ForceUpdateStateAndTryGenerateEffect);

    /// <summary>
	/// Bindable property for <see cref="ShouldSetImageOnAnimationEnd"/>
	/// </summary>
    public readonly static BindableProperty ShouldSetImageOnAnimationEndProperty = BindableProperty.CreateAttached(
        nameof(ShouldSetImageOnAnimationEnd),
        typeof(bool),
        typeof(TouchEffect),
        default(bool),
        propertyChanged: TryGenerateEffect);

    #region Native Animation

    public readonly static BindableProperty PulseCountProperty = BindableProperty.CreateAttached(
		nameof(PulseCount),
		typeof(int),
		typeof(TouchEffect),
		default(int),
		propertyChanged: ForceUpdateStateAndTryGenerateEffect);

	public readonly static BindableProperty IsToggledProperty = BindableProperty.CreateAttached(
		nameof(IsToggled),
		typeof(bool?),
		typeof(TouchEffect),
		default(bool?),
		BindingMode.TwoWay,
		propertyChanged: ForceUpdateStateWithoutAnimationAndTryGenerateEffect);

	public readonly static BindableProperty NativeAnimationProperty = BindableProperty.CreateAttached(
		nameof(NativeAnimation),
		typeof(bool),
		typeof(TouchEffect),
		false,
		propertyChanged: TryGenerateEffect);

	public readonly static BindableProperty NativeAnimationColorProperty = BindableProperty.CreateAttached(
		nameof(NativeAnimationColor),
		typeof(Color),
		typeof(TouchEffect),
        KnownColor.Default,
		propertyChanged: TryGenerateEffect);

	public readonly static BindableProperty NativeAnimationRadiusProperty = BindableProperty.CreateAttached(
		nameof(NativeAnimationRadius),
		typeof(int),
		typeof(TouchEffect),
		-1,
		propertyChanged: TryGenerateEffect);

	public readonly static BindableProperty NativeAnimationShadowRadiusProperty = BindableProperty.CreateAttached(
		nameof(NativeAnimationShadowRadius),
		typeof(int),
		typeof(TouchEffect),
		-1,
		propertyChanged: TryGenerateEffect);

	public readonly static BindableProperty NativeAnimationBorderlessProperty = BindableProperty.CreateAttached(
		nameof(NativeAnimationBorderless),
		typeof(bool),
		typeof(TouchEffect),
		false,
		propertyChanged: TryGenerateEffect);

    #endregion Native Animation

    #endregion Bindable Properties

    #region Properties

    internal bool IsDisabled { get; set; }

    internal bool IsUsed { get; set; }

    internal bool IsAutoGenerated { get; set; }

    public bool IsEnabled => GetIsEnabled(Element);

    public bool ShouldMakeChildrenInputTransparent => GetShouldMakeChildrenInputTransparent(Element);

    public ICommand? Command => GetCommand(Element);

    public ICommand? LongPressCommand => GetLongPressCommand(Element);

    public object? CommandParameter => GetCommandParameter(Element);

    public object? LongPressCommandParameter => GetLongPressCommandParameter(Element);

    public TimeSpan LongPressDuration => GetLongPressDuration(Element);

    public ImageSource? DefaultImageSource => GetDefaultImageSource(Element);

    public ImageSource? HoveredImageSource => GetHoveredImageSource(Element);

    public ImageSource? PressedImageSource => GetPressedImageSource(Element);

    public Aspect DefaultImageAspect => GetDefaultImageAspect(Element);

    public Aspect HoveredImageAspect => GetHoveredImageAspect(Element);

    public Aspect PressedImageAspect => GetPressedImageAspect(Element);

    public bool ShouldSetImageOnAnimationEnd => GetShouldSetImageOnAnimationEnd(Element);

    public TouchStatus CurrentTouchStatus
    {
        get => GetCurrentTouchStatus(Element);
        internal set => SetCurrentTouchStatus(Element, value);
    }

    public TouchState CurrentTouchState
    {
        get => GetCurrentTouchState(Element);
        internal set => SetCurrentTouchState(Element, value);
    }

    public TouchInteractionStatus CurrentInteractionStatus
    {
        get => GetCurrentInteractionStatus(Element);
        internal set => SetCurrentInteractionStatus(Element, value);
    }

    public HoverStatus CurrentHoverStatus
    {
        get => GetCurrentHoverStatus(Element);
        internal set => SetCurrentHoverStatus(Element, value);
    }

    public HoverState CurrentHoverState
    {
        get => GetCurrentHoverState(Element);
        internal set => SetCurrentHoverState(Element, value);
    }

    public int DisallowTouchThreshold => GetDisallowTouchThreshold(Element);

    public bool NativeAnimation => GetNativeAnimation(Element);

    public Color? NativeAnimationColor => GetNativeAnimationColor(Element);

    public int NativeAnimationRadius => GetNativeAnimationRadius(Element);

    public int NativeAnimationShadowRadius => GetNativeAnimationShadowRadius(Element);

    public bool NativeAnimationBorderless => GetNativeAnimationBorderless(Element);

    public Color? DefaultBackgroundColor => GetDefaultBackgroundColor(Element);

    public Color? HoveredBackgroundColor => GetHoveredBackgroundColor(Element);

    public Color? PressedBackgroundColor => GetPressedBackgroundColor(Element);

    public double DefaultOpacity => GetDefaultOpacity(Element);

    public double HoveredOpacity => GetHoveredOpacity(Element);

    public double PressedOpacity => GetPressedOpacity(Element);

    public double DefaultScale => GetDefaultScale(Element);

    public double HoveredScale => GetHoveredScale(Element);

    public double PressedScale => GetPressedScale(Element);

    public double DefaultTranslationX => GetDefaultTranslationX(Element);

    public double HoveredTranslationX => GetHoveredTranslationX(Element);

    public double PressedTranslationX => GetPressedTranslationX(Element);

    public double DefaultTranslationY => GetDefaultTranslationY(Element);

    public double HoveredTranslationY => GetHoveredTranslationY(Element);

    public double PressedTranslationY => GetPressedTranslationY(Element);

    public double DefaultRotation => GetDefaultRotation(Element);

    public double HoveredRotation => GetHoveredRotation(Element);

    public double PressedRotation => GetPressedRotation(Element);

    public double DefaultRotationX => GetDefaultRotationX(Element);

    public double HoveredRotationX => GetHoveredRotationX(Element);

    public double PressedRotationX => GetPressedRotationX(Element);

    public double DefaultRotationY => GetDefaultRotationY(Element);

    public double HoveredRotationY => GetHoveredRotationY(Element);

    public double PressedRotationY => GetPressedRotationY(Element);

    public int PressedAnimationDuration => GetPressedAnimationDuration(Element);

    public Easing? PressedAnimationEasing => GetPressedAnimationEasing(Element);

    public int DefaultAnimationDuration => GetDefaultAnimationDuration(Element);

    public Easing? DefaultAnimationEasing => GetDefaultAnimationEasing(Element);

    public int HoveredAnimationDuration => GetHoveredAnimationDuration(Element);

    public Easing? HoveredAnimationEasing => GetHoveredAnimationEasing(Element);

    public int PulseCount => GetPulseCount(Element);

    public bool? IsToggled
    {
        get => GetIsToggled(Element);
        internal set => SetIsToggled(Element, value);
    }

    internal bool CanExecute => IsEnabled
                                && (Element?.IsEnabled ?? false)
                                && (Command?.CanExecute(CommandParameter) ?? true);

    private VisualElement? element;

    internal new VisualElement? Element
    {
        get => element;
        set
        {
            if (element != null)
            {
                IsUsed = false;
                gestureManager.Reset();
                SetChildrenInputTransparent(false);
            }

            gestureManager.AbortAnimations(this);
            element = value;
            if (value != null)
            {
                SetChildrenInputTransparent(ShouldMakeChildrenInputTransparent);
                if (!IsAutoGenerated)
                {
                    IsUsed = true;
                    foreach (var effect in value.Effects.OfType<TouchEffect>())
                    {
                        effect.IsDisabled = effect != this;
                    }
                }

                ForceUpdateState();
            }
        }
    }

    #endregion Properties

    #region Events

    public event EventHandler<TouchStatusChangedEventArgs> CurrentTouchStatusChanged
    {
        add => weakEventManager.AddEventHandler(value);
        remove => weakEventManager.RemoveEventHandler(value);
    }

    public event EventHandler<TouchStateChangedEventArgs> CurrentTouchStateChanged
    {
        add => weakEventManager.AddEventHandler(value);
        remove => weakEventManager.RemoveEventHandler(value);
    }

    public event EventHandler<TouchInteractionStatusChangedEventArgs> InteractionStatusChanged
    {
        add => weakEventManager.AddEventHandler(value);
        remove => weakEventManager.RemoveEventHandler(value);
    }

    public event EventHandler<HoverStatusChangedEventArgs> HoverStatusChanged
    {
        add => weakEventManager.AddEventHandler(value);
        remove => weakEventManager.RemoveEventHandler(value);
    }

    public event EventHandler<HoverStateChangedEventArgs> HoverStateChanged
    {
        add => weakEventManager.AddEventHandler(value);
        remove => weakEventManager.RemoveEventHandler(value);
    }

    public event EventHandler<TouchCompletedEventArgs> TouchGestureCompleted
    {
        add => weakEventManager.AddEventHandler(value);
        remove => weakEventManager.RemoveEventHandler(value);
    }

    public event EventHandler<LongPressCompletedEventArgs> LongPressCompleted
    {
        add => weakEventManager.AddEventHandler(value);
        remove => weakEventManager.RemoveEventHandler(value);
    }

    #endregion Events

    private readonly GestureManager gestureManager = new();

	private readonly WeakEventManager weakEventManager = new();

    #region Property Get Set Methods

    public static bool GetIsEnabled(BindableObject? bindable)
    {
        return (bool)(bindable?.GetValue(IsEnabledProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetIsEnabled(BindableObject? bindable, bool value)
    {
        bindable?.SetValue(IsEnabledProperty, value);
    }

    public static bool GetShouldMakeChildrenInputTransparent(BindableObject? bindable)
    {
        return (bool)(bindable?.GetValue(ShouldMakeChildrenInputTransparentProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetShouldMakeChildrenInputTransparent(BindableObject? bindable, bool value)
    {
        bindable?.SetValue(ShouldMakeChildrenInputTransparentProperty, value);
    }

    public static ICommand? GetCommand(BindableObject? bindable)
    {
        if (bindable == null)
        {
            throw new ArgumentNullException(nameof(bindable));
        }

        return (ICommand?)bindable.GetValue(CommandProperty);
    }

    public static void SetCommand(BindableObject? bindable, ICommand value)
    {
        bindable?.SetValue(CommandProperty, value);
    }

    public static ICommand? GetLongPressCommand(BindableObject? bindable)
    {
        if (bindable == null)
        {
            throw new ArgumentNullException(nameof(bindable));
        }

        return (ICommand?)bindable.GetValue(LongPressCommandProperty);
    }

    public static void SetLongPressCommand(BindableObject? bindable, ICommand value)
    {
        bindable?.SetValue(LongPressCommandProperty, value);
    }

    public static object? GetCommandParameter(BindableObject? bindable)
    {
        if (bindable == null)
        {
            throw new ArgumentNullException(nameof(bindable));
        }

        return bindable.GetValue(CommandParameterProperty);
    }

    public static void SetCommandParameter(BindableObject? bindable, object value)
    {
        bindable?.SetValue(CommandParameterProperty, value);
    }

    public static object? GetLongPressCommandParameter(BindableObject? bindable)
    {
        if (bindable == null)
        {
            throw new ArgumentNullException(nameof(bindable));
        }

        return bindable.GetValue(LongPressCommandParameterProperty);
    }

    public static void SetLongPressCommandParameter(BindableObject? bindable, object value)
    {
        bindable?.SetValue(LongPressCommandParameterProperty, value);
    }

    [TypeConverter(typeof(TimeSpanMillisecondTypeConverter))]
    public static TimeSpan GetLongPressDuration(BindableObject? bindable)
    {
        return (TimeSpan)(bindable?.GetValue(LongPressDurationProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    [TypeConverter(typeof(TimeSpanMillisecondTypeConverter))]
    public static void SetLongPressDuration(BindableObject? bindable, int value)
    {
        bindable?.SetValue(LongPressDurationProperty, value);
    }

    public static TouchStatus GetCurrentTouchStatus(BindableObject? bindable)
    {
        return (TouchStatus)(bindable?.GetValue(CurrentTouchStatusProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetCurrentTouchStatus(BindableObject? bindable, TouchStatus value)
    {
        bindable?.SetValue(CurrentTouchStatusProperty, value);
    }

    public static TouchState GetCurrentTouchState(BindableObject? bindable)
    {
        return (TouchState)(bindable?.GetValue(CurrentTouchStateProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetCurrentTouchState(BindableObject? bindable, TouchState value)
    {
        bindable?.SetValue(CurrentTouchStateProperty, value);
    }

    public static TouchInteractionStatus GetCurrentInteractionStatus(BindableObject? bindable)
    {
        return (TouchInteractionStatus)(bindable?.GetValue(CurrentInteractionStatusProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetCurrentInteractionStatus(BindableObject? bindable, TouchInteractionStatus value)
    {
        bindable?.SetValue(CurrentInteractionStatusProperty, value);
    }

    public static HoverStatus GetCurrentHoverStatus(BindableObject? bindable)
    {
        return (HoverStatus)(bindable?.GetValue(CurrentHoverStatusProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetCurrentHoverStatus(BindableObject? bindable, HoverStatus value)
    {
        bindable?.SetValue(CurrentHoverStatusProperty, value);
    }

    public static HoverState GetCurrentHoverState(BindableObject? bindable)
    {
        return (HoverState)(bindable?.GetValue(CurrentHoverStateProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetCurrentHoverState(BindableObject? bindable, HoverState value)
    {
        bindable?.SetValue(CurrentHoverStateProperty, value);
    }

    public static Color? GetDefaultBackgroundColor(BindableObject? bindable)
    {
        return bindable?.GetValue(DefaultBackgroundColorProperty) as Color;
    }

    public static void SetNormalBackgroundColor(BindableObject? bindable, Color value)
    {
        bindable?.SetValue(DefaultBackgroundColorProperty, value);
    }

    public static Color? GetHoveredBackgroundColor(BindableObject? bindable)
    {
        return bindable?.GetValue(HoveredBackgroundColorProperty) as Color;
    }

    public static void SetHoveredBackgroundColor(BindableObject? bindable, Color value)
    {
        bindable?.SetValue(HoveredBackgroundColorProperty, value);
    }

    public static Color? GetPressedBackgroundColor(BindableObject? bindable)
    {
        return bindable?.GetValue(PressedBackgroundColorProperty) as Color;
    }

    public static void SetPressedBackgroundColor(BindableObject? bindable, Color value)
    {
        bindable?.SetValue(PressedBackgroundColorProperty, value);
    }

    public static double GetDefaultOpacity(BindableObject? bindable)
    {
        return (double)(bindable?.GetValue(DefaultOpacityProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetNormalOpacity(BindableObject? bindable, double value)
    {
        bindable?.SetValue(DefaultOpacityProperty, value);
    }

    public static double GetHoveredOpacity(BindableObject? bindable)
    {
        return (double)(bindable?.GetValue(HoveredOpacityProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetHoveredOpacity(BindableObject? bindable, double value)
    {
        bindable?.SetValue(HoveredOpacityProperty, value);
    }

    public static double GetPressedOpacity(BindableObject? bindable)
    {
        return (double)(bindable?.GetValue(PressedOpacityProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetPressedOpacity(BindableObject? bindable, double value)
    {
        bindable?.SetValue(PressedOpacityProperty, value);
    }

    public static double GetDefaultScale(BindableObject? bindable)
    {
        return (double)(bindable?.GetValue(DefaultScaleProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetNormalScale(BindableObject? bindable, double value)
    {
        bindable?.SetValue(DefaultScaleProperty, value);
    }

    public static double GetHoveredScale(BindableObject? bindable)
    {
        return (double)(bindable?.GetValue(HoveredScaleProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetHoveredScale(BindableObject? bindable, double value)
    {
        bindable?.SetValue(HoveredScaleProperty, value);
    }

    public static double GetPressedScale(BindableObject? bindable)
    {
        return (double)(bindable?.GetValue(PressedScaleProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetPressedScale(BindableObject? bindable, double value)
    {
        bindable?.SetValue(PressedScaleProperty, value);
    }

    public static double GetDefaultTranslationX(BindableObject? bindable)
    {
        return (double)(bindable?.GetValue(DefaultTranslationXProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetNormalTranslationX(BindableObject? bindable, double value)
    {
        bindable?.SetValue(DefaultTranslationXProperty, value);
    }

    public static double GetHoveredTranslationX(BindableObject? bindable)
    {
        return (double)(bindable?.GetValue(HoveredTranslationXProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetHoveredTranslationX(BindableObject? bindable, double value)
    {
        bindable?.SetValue(HoveredTranslationXProperty, value);
    }

    public static double GetPressedTranslationX(BindableObject? bindable)
    {
        return (double)(bindable?.GetValue(PressedTranslationXProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetPressedTranslationX(BindableObject? bindable, double value)
    {
        bindable?.SetValue(PressedTranslationXProperty, value);
    }

    public static double GetDefaultTranslationY(BindableObject? bindable)
    {
        return (double)(bindable?.GetValue(DefaultTranslationYProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetNormalTranslationY(BindableObject? bindable, double value)
    {
        bindable?.SetValue(DefaultTranslationYProperty, value);
    }

    public static double GetHoveredTranslationY(BindableObject? bindable)
    {
        return (double)(bindable?.GetValue(HoveredTranslationYProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetHoveredTranslationY(BindableObject? bindable, double value)
    {
        bindable?.SetValue(HoveredTranslationYProperty, value);
    }

    public static double GetPressedTranslationY(BindableObject? bindable)
    {
        return (double)(bindable?.GetValue(PressedTranslationYProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetPressedTranslationY(BindableObject? bindable, double value)
    {
        bindable?.SetValue(PressedTranslationYProperty, value);
    }

    public static double GetDefaultRotation(BindableObject? bindable)
    {
        return (double)(bindable?.GetValue(DefaultRotationProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetNormalRotation(BindableObject? bindable, double value)
    {
        bindable?.SetValue(DefaultRotationProperty, value);
    }

    public static double GetHoveredRotation(BindableObject? bindable)
    {
        return (double)(bindable?.GetValue(HoveredRotationProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetHoveredRotation(BindableObject? bindable, double value)
    {
        bindable?.SetValue(HoveredRotationProperty, value);
    }

    public static double GetPressedRotation(BindableObject? bindable)
    {
        return (double)(bindable?.GetValue(PressedRotationProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetPressedRotation(BindableObject? bindable, double value)
    {
        bindable?.SetValue(PressedRotationProperty, value);
    }

    public static double GetDefaultRotationX(BindableObject? bindable)
    {
        return (double)(bindable?.GetValue(DefaultRotationXProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetNormalRotationX(BindableObject? bindable, double value)
    {
        bindable?.SetValue(DefaultRotationXProperty, value);
    }

    public static double GetHoveredRotationX(BindableObject? bindable)
    {
        return (double)(bindable?.GetValue(HoveredRotationXProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetHoveredRotationX(BindableObject? bindable, double value)
    {
        bindable?.SetValue(HoveredRotationXProperty, value);
    }

    public static double GetPressedRotationX(BindableObject? bindable)
    {
        return (double)(bindable?.GetValue(PressedRotationXProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetPressedRotationX(BindableObject? bindable, double value)
    {
        bindable?.SetValue(PressedRotationXProperty, value);
    }

    public static double GetDefaultRotationY(BindableObject? bindable)
    {
        return (double)(bindable?.GetValue(DefaultRotationYProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetNormalRotationY(BindableObject? bindable, double value)
    {
        bindable?.SetValue(DefaultRotationYProperty, value);
    }

    public static double GetHoveredRotationY(BindableObject? bindable)
    {
        return (double)(bindable?.GetValue(HoveredRotationYProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetHoveredRotationY(BindableObject? bindable, double value)
    {
        bindable?.SetValue(HoveredRotationYProperty, value);
    }

    public static double GetPressedRotationY(BindableObject? bindable)
    {
        return (double)(bindable?.GetValue(PressedRotationYProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetPressedRotationY(BindableObject? bindable, double value)
    {
        bindable?.SetValue(PressedRotationYProperty, value);
    }

    public static int GetPressedAnimationDuration(BindableObject? bindable)
    {
        return (int)(bindable?.GetValue(PressedAnimationDurationProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetPressedAnimationDuration(BindableObject? bindable, int value)
    {
        bindable?.SetValue(PressedAnimationDurationProperty, value);
    }

    public static Easing? GetPressedAnimationEasing(BindableObject? bindable)
    {
        if (bindable == null)
        {
            throw new ArgumentNullException(nameof(bindable));
        }

        return (Easing?)bindable.GetValue(PressedAnimationEasingProperty);
    }

    public static void SetPressedAnimationEasing(BindableObject? bindable, Easing? value)
    {
        bindable?.SetValue(PressedAnimationEasingProperty, value);
    }

    public static int GetDefaultAnimationDuration(BindableObject? bindable)
    {
        return (int)(bindable?.GetValue(DefaultAnimationDurationProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetNormalAnimationDuration(BindableObject? bindable, int value)
    {
        bindable?.SetValue(DefaultAnimationDurationProperty, value);
    }

    public static Easing? GetDefaultAnimationEasing(BindableObject? bindable)
    {
        if (bindable == null)
        {
            throw new ArgumentNullException(nameof(bindable));
        }

        return (Easing?)bindable.GetValue(DefaultAnimationEasingProperty);
    }

    public static void SetNormalAnimationEasing(BindableObject? bindable, Easing? value)
    {
        bindable?.SetValue(DefaultAnimationEasingProperty, value);
    }

    public static int GetHoveredAnimationDuration(BindableObject? bindable)
    {
        return (int)(bindable?.GetValue(HoveredAnimationDurationProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetHoveredAnimationDuration(BindableObject? bindable, int value)
    {
        bindable?.SetValue(HoveredAnimationDurationProperty, value);
    }

    public static Easing? GetHoveredAnimationEasing(BindableObject? bindable)
    {
        if (bindable == null)
        {
            throw new ArgumentNullException(nameof(bindable));
        }

        return (Easing?)bindable.GetValue(HoveredAnimationEasingProperty);
    }

    public static void SetHoveredAnimationEasing(BindableObject? bindable, Easing? value)
    {
        bindable?.SetValue(HoveredAnimationEasingProperty, value);
    }

    public static int GetPulseCount(BindableObject? bindable)
    {
        return (int)(bindable?.GetValue(PulseCountProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetPulseCount(BindableObject? bindable, int value)
    {
        bindable?.SetValue(PulseCountProperty, value);
    }

    public static bool? GetIsToggled(BindableObject? bindable)
    {
        if (bindable == null)
        {
            throw new ArgumentNullException(nameof(bindable));
        }

        return (bool?)bindable.GetValue(IsToggledProperty);
    }

    public static void SetIsToggled(BindableObject? bindable, bool? value)
    {
        bindable?.SetValue(IsToggledProperty, value);
    }

    public static int GetDisallowTouchThreshold(BindableObject? bindable)
    {
        return (int)(bindable?.GetValue(DisallowTouchThresholdProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetDisallowTouchThreshold(BindableObject? bindable, int value)
    {
        bindable?.SetValue(DisallowTouchThresholdProperty, value);
    }

    public static bool GetNativeAnimation(BindableObject? bindable)
    {
        return (bool)(bindable?.GetValue(NativeAnimationProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetNativeAnimation(BindableObject? bindable, bool value)
    {
        bindable?.SetValue(NativeAnimationProperty, value);
    }

    public static Color? GetNativeAnimationColor(BindableObject? bindable)
    {
        return bindable?.GetValue(NativeAnimationColorProperty) as Color;
    }

    public static void SetNativeAnimationColor(BindableObject? bindable, Color value)
    {
        bindable?.SetValue(NativeAnimationColorProperty, value);
    }

    public static int GetNativeAnimationRadius(BindableObject? bindable)
    {
        return (int)(bindable?.GetValue(NativeAnimationRadiusProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetNativeAnimationRadius(BindableObject? bindable, int value)
    {
        bindable?.SetValue(NativeAnimationRadiusProperty, value);
    }

    public static int GetNativeAnimationShadowRadius(BindableObject? bindable)
    {
        return (int)(bindable?.GetValue(NativeAnimationShadowRadiusProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetNativeAnimationShadowRadius(BindableObject? bindable, int value)
    {
        bindable?.SetValue(NativeAnimationShadowRadiusProperty, value);
    }

    public static bool GetNativeAnimationBorderless(BindableObject? bindable)
    {
        return (bool)(bindable?.GetValue(NativeAnimationBorderlessProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetNativeAnimationBorderless(BindableObject? bindable, bool value)
    {
        bindable?.SetValue(NativeAnimationBorderlessProperty, value);
    }

    public static ImageSource? GetDefaultImageSource(BindableObject? bindable)
    {
        if (bindable == null)
        {
            throw new ArgumentNullException(nameof(bindable));
        }

        return (ImageSource?)bindable.GetValue(DefaultImageSourceProperty);
    }

    public static void SetDefaultImageSource(BindableObject? bindable, ImageSource value)
    {
        bindable?.SetValue(DefaultImageSourceProperty, value);
    }

    public static ImageSource? GetHoveredImageSource(BindableObject? bindable)
    {
        if (bindable == null)
        {
            throw new ArgumentNullException(nameof(bindable));
        }

        return (ImageSource?)bindable.GetValue(HoveredImageSourceProperty);
    }

    public static void SetHoveredImageSource(BindableObject? bindable, ImageSource value)
    {
        bindable?.SetValue(HoveredImageSourceProperty, value);
    }

    public static ImageSource? GetPressedImageSource(BindableObject? bindable)
    {
        if (bindable == null)
        {
            throw new ArgumentNullException(nameof(bindable));
        }

        return (ImageSource?)bindable.GetValue(PressedImageSourceProperty);
    }

    public static void SetPressedImageSource(BindableObject? bindable, ImageSource value)
    {
        bindable?.SetValue(PressedImageSourceProperty, value);
    }

    public static Aspect GetDefaultImageAspect(BindableObject? bindable)
    {
        return (Aspect)(bindable?.GetValue(DefaultImageAspectProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetDefaultImageAspect(BindableObject? bindable, Aspect value)
    {
        bindable?.SetValue(DefaultImageAspectProperty, value);
    }

    public static Aspect GetHoveredImageAspect(BindableObject? bindable)
    {
        return (Aspect)(bindable?.GetValue(HoveredImageAspectProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetHoveredImageAspect(BindableObject? bindable, Aspect value)
    {
        bindable?.SetValue(HoveredImageAspectProperty, value);
    }

    public static Aspect GetPressedImageAspect(BindableObject? bindable)
    {
        return (Aspect)(bindable?.GetValue(PressedImageAspectProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetPressedImageAspect(BindableObject? bindable, Aspect value)
    {
        bindable?.SetValue(PressedImageAspectProperty, value);
    }

    public static bool GetShouldSetImageOnAnimationEnd(BindableObject? bindable)
    {
        return (bool)(bindable?.GetValue(ShouldSetImageOnAnimationEndProperty) ?? throw new ArgumentNullException(nameof(bindable)));
    }

    public static void SetShouldSetImageOnAnimationEnd(BindableObject? bindable, bool value)
    {
        bindable?.SetValue(ShouldSetImageOnAnimationEndProperty, value);
    }

    #endregion Property Get Set Methods
}
