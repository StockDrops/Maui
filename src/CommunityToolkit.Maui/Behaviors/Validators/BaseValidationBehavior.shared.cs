using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>Validation flags</summary>
[Flags]
public enum ValidationFlags
{
	/// <summary> No validation</summary>
	None = 0,
	/// <summary>Validate on attaching</summary>
	ValidateOnAttaching = 1,
	/// <summary> Validate on focusing</summary>
	ValidateOnFocusing = 2,
	/// <summary>Validate on unfocusing</summary>
	ValidateOnUnfocusing = 4,
	/// <summary>Validate upon value changed</summary>
	ValidateOnValueChanged = 8,
	/// <summary>Force make valid when focused</summary>
	ForceMakeValidWhenFocused = 16
}

/// <summary>
/// The <see cref="BaseValidationBehavior"/> allows users to create custom validation behaviors.
/// This class will not make assumptions about which value it will bind itself too. I want to be able to bind myself to anything, specially multibinding scenarios.
/// So this won't bind value to anything but instead offer an abstract override that allows the user to define this binding.
/// All of the validation behaviors in the Xamarin Community Toolkit inherit from this behavior, to expose a number of shared properties.
/// Users can inherit from this class to create a custom validation behavior currently not supported through the Xamarin Community Toolkit. 
/// This behavios cannot be used directly as it's abstract.
/// </summary>
public abstract class BaseValidationBehavior : BaseBehavior<VisualElement>
{
	/// <summary>
	/// Valid visual state
	/// </summary>
	public const string ValidVisualState = "Valid";

	/// <summary>
	/// Invalid visual state
	/// </summary>
	public const string InvalidVisualState = "Invalid";

	/// <summary>
	/// Backing BindableProperty for the <see cref="IsNotValid"/> property.
	/// </summary>
	public static readonly BindableProperty IsNotValidProperty =
		BindableProperty.Create(nameof(IsNotValid), typeof(bool), typeof(ValidationBehavior), false, BindingMode.OneWayToSource);

	/// <summary>
	/// Backing BindableProperty for the <see cref="IsValid"/> property.
	/// </summary>
	public static readonly BindableProperty IsValidProperty =
		BindableProperty.Create(nameof(IsValid), typeof(bool), typeof(ValidationBehavior), true, BindingMode.OneWayToSource, propertyChanged: OnIsValidPropertyChanged);

	/// <summary>
	/// Backing BindableProperty for the <see cref="IsRunning"/> property.
	/// </summary>
	public static readonly BindableProperty IsRunningProperty =
		BindableProperty.Create(nameof(IsRunning), typeof(bool), typeof(ValidationBehavior), false, BindingMode.OneWayToSource);

	/// <summary>
	/// Backing BindableProperty for the <see cref="ValidStyle"/> property.
	/// </summary>
	public static readonly BindableProperty ValidStyleProperty =
		BindableProperty.Create(nameof(ValidStyle), typeof(Style), typeof(ValidationBehavior), propertyChanged: OnValidationPropertyChanged);

	/// <summary>
	/// Backing BindableProperty for the <see cref="InvalidStyle"/> property.
	/// </summary>
	public static readonly BindableProperty InvalidStyleProperty =
		BindableProperty.Create(nameof(InvalidStyle), typeof(Style), typeof(ValidationBehavior), propertyChanged: OnValidationPropertyChanged);

	/// <summary>
	/// Backing BindableProperty for the <see cref="Flags"/> property.
	/// </summary>
	public static readonly BindableProperty FlagsProperty =
		BindableProperty.Create(nameof(Flags), typeof(ValidationFlags), typeof(ValidationBehavior), ValidationFlags.ValidateOnUnfocusing | ValidationFlags.ForceMakeValidWhenFocused, propertyChanged: OnValidationPropertyChanged);

	/// <summary>
	/// Backing BindableProperty for the <see cref="Value"/> property.
	/// </summary>
	public static readonly BindableProperty ValueProperty =
		BindableProperty.Create(nameof(Value), typeof(object), typeof(ValidationBehavior), propertyChanged: OnValuePropertyChanged);

	/// <summary>
	/// Backing BindableProperty for the <see cref="ForceValidateCommand"/> property.
	/// </summary>
	public static readonly BindableProperty ForceValidateCommandProperty =
		BindableProperty.Create(nameof(ForceValidateCommand), typeof(ICommand), typeof(ValidationBehavior), defaultValueCreator: GetDefaultForceValidateCommand, defaultBindingMode: BindingMode.OneWayToSource);

	readonly SemaphoreSlim _isAttachingSemaphoreSlim = new(1, 1);

	ValidationFlags _currentStatus;

	BindingBase? _defaultValueBinding;

	CancellationTokenSource? _validationTokenSource;

	/// <summary>
	/// Initialize a new instance of ValidationBehavior
	/// </summary>
	public BaseValidationBehavior() => DefaultForceValidateCommand = new Command(async () => await ForceValidate().ConfigureAwait(false));

	/// <summary>
	/// Indicates whether or not the current value is considered valid. This is a bindable property.
	/// </summary>
	public bool IsValid
	{
		get => (bool)GetValue(IsValidProperty);
		set => SetValue(IsValidProperty, value);
	}

	/// <summary>
	/// Indicates whether or not the validation is in progress now (waiting for an asynchronous call is finished).
	/// </summary>
	public bool IsRunning
	{
		get => (bool)GetValue(IsRunningProperty);
		set => SetValue(IsRunningProperty, value);
	}

	/// <summary>
	/// Indicates whether or not the current value is considered not valid. This is a bindable property.
	/// </summary>
	public bool IsNotValid
	{
		get => (bool)GetValue(IsNotValidProperty);
		set => SetValue(IsNotValidProperty, value);
	}

	/// <summary>
	/// The <see cref="Style"/> to apply to the element when validation is successful. This is a bindable property.
	/// </summary>
	public Style? ValidStyle
	{
		get => (Style?)GetValue(ValidStyleProperty);
		set => SetValue(ValidStyleProperty, value);
	}

	/// <summary>
	/// The <see cref="Style"/> to apply to the element when validation fails. This is a bindable property.
	/// </summary>
	public Style? InvalidStyle
	{
		get => (Style?)GetValue(InvalidStyleProperty);
		set => SetValue(InvalidStyleProperty, value);
	}

	/// <summary>
	/// Provides an enumerated value that specifies how to handle validation. This is a bindable property.
	/// </summary>
	public ValidationFlags Flags
	{
		get => (ValidationFlags)GetValue(FlagsProperty);
		set => SetValue(FlagsProperty, value);
	}

	/// <summary>
	/// The value to validate. This is a bindable property.
	/// </summary>
	public object? Value
	{
		get => (object?)GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}

	/// <summary>
	/// Allows the user to provide a custom <see cref="ICommand"/> that handles forcing validation. This is a bindable property.
	/// </summary>
	public ICommand? ForceValidateCommand
	{
		get => (ICommand?)GetValue(ForceValidateCommandProperty);
		set => SetValue(ForceValidateCommandProperty, value);
	}

	/// <summary>
	/// Default force validate command
	/// </summary>
	protected virtual ICommand DefaultForceValidateCommand { get; }

	/// <summary>
	/// Forces the behavior to make a validation pass.
	/// </summary>
	public ValueTask ForceValidate() => UpdateStateAsync(View, Flags, true);

	internal ValueTask ValidateNestedAsync(CancellationToken token) => UpdateStateAsync(View, Flags, true, token);

	/// <summary>
	/// Decorate value
	/// </summary>
	protected virtual object? Decorate(object? value) => value;

	/// <summary>
	/// Validate value
	/// </summary>
	protected abstract ValueTask<bool> ValidateAsync(object? value, CancellationToken token);

	/// <inheritdoc/>
	protected override async void OnAttachedTo(VisualElement bindable)
	{
		base.OnAttachedTo(bindable);

		await _isAttachingSemaphoreSlim.WaitAsync();

		try
		{
			_currentStatus = ValidationFlags.ValidateOnAttaching;

			ConfigureValueBinding();
			await UpdateStateAsync(View, Flags, false).ConfigureAwait(false);
		}
		finally
		{
			_isAttachingSemaphoreSlim.Release();
		}
	}

	/// <inheritdoc/>
	protected override void OnDetachingFrom(VisualElement bindable)
	{
		if (_defaultValueBinding != null)
		{
			RemoveBinding(ValueProperty);
			_defaultValueBinding = null;
		}

		_currentStatus = ValidationFlags.None;
		base.OnDetachingFrom(bindable);
	}

	/// <inheritdoc/>
	protected override async void OnViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		base.OnViewPropertyChanged(sender, e);

		if (e.PropertyName == VisualElement.IsFocusedProperty.PropertyName)
		{
			var view = (VisualElement?)sender;

			_currentStatus = view?.IsFocused switch
			{
				true => ValidationFlags.ValidateOnFocusing,
				_ => ValidationFlags.ValidateOnUnfocusing
			};

			await UpdateStateAsync(View, Flags, false).ConfigureAwait(false);
		}
	}

	/// <inheritdoc/>
	protected static async void OnValidationPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var validationBehavior = (BaseValidationBehavior)bindable;
		await validationBehavior.UpdateStateAsync(validationBehavior.View, validationBehavior.Flags, false).ConfigureAwait(false);
	}

	static void OnIsValidPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		=> ((BaseValidationBehavior)bindable).OnIsValidPropertyChanged();

	static async void OnValuePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		await ((BaseValidationBehavior)bindable).OnValuePropertyChanged();
		OnValidationPropertyChanged(bindable, oldValue, newValue);
	}

	static object GetDefaultForceValidateCommand(BindableObject bindable)
		=> ((BaseValidationBehavior)bindable).DefaultForceValidateCommand;

	//static object GetDefaultValuePropertyName(BindableObject bindable) //TODO: replace this with a GetDefaultBinding? Maybe?
	//	=> ((BaseValidationBehavior)bindable).DefaultValuePropertyName;

	void OnIsValidPropertyChanged() => IsNotValid = !IsValid;

	async Task OnValuePropertyChanged()
	{
		await _isAttachingSemaphoreSlim.WaitAsync();

		try
		{
			_currentStatus = ValidationFlags.ValidateOnValueChanged;
		}
		finally
		{
			_isAttachingSemaphoreSlim.Release();
		}
	}
	/// <summary>
	/// You can call this method to override how the default binding is done for the ValueProperty.
	/// </summary>
	protected abstract BindingBase CreateBinding();
	/// <summary>
	/// You can override this method to change how the ValueProperty binding is done. 
	/// </summary>
	protected virtual void ConfigureValueBinding()
	{
		if (IsBound(ValueProperty, _defaultValueBinding))
		{
			_defaultValueBinding = null;
			return;
		}

		_defaultValueBinding = CreateBinding();
		SetBinding(ValueProperty, _defaultValueBinding);
	}

	async ValueTask UpdateStateAsync(VisualElement? view, ValidationFlags flags, bool isForced, CancellationToken? parentToken = null)
	{
		if (parentToken?.IsCancellationRequested is true)
			return;

		if ((view?.IsFocused ?? false) && flags.HasFlag(ValidationFlags.ForceMakeValidWhenFocused))
		{
			IsRunning = true;

			ResetValidationTokenSource(null);

			IsValid = true;
			IsRunning = false;
		}
		else if (isForced || (_currentStatus != ValidationFlags.None && Flags.HasFlag(_currentStatus)))
		{
			IsRunning = true;

			using var tokenSource = new CancellationTokenSource();
			var token = parentToken ?? tokenSource.Token;
			ResetValidationTokenSource(tokenSource);

			try
			{
				var isValid = await ValidateAsync(Decorate(Value), token);

				if (token.IsCancellationRequested)
					return;

				_validationTokenSource = null;
				IsValid = isValid;
				IsRunning = false;
			}
			catch (TaskCanceledException)
			{
				return;
			}
		}

		if (view is not null)
			UpdateStyle(view, IsValid);
	}

	void UpdateStyle(in VisualElement view, bool isValid)
	{
		VisualStateManager.GoToState(view, isValid ? ValidVisualState : InvalidVisualState);

		if ((ValidStyle ?? InvalidStyle) == null)
			return;

		view.Style = isValid ? ValidStyle : InvalidStyle;
	}

	void ResetValidationTokenSource(CancellationTokenSource? newTokenSource)
	{
		_validationTokenSource?.Cancel();
		_validationTokenSource = newTokenSource;
	}
}