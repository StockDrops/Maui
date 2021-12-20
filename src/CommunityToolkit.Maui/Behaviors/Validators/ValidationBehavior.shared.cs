using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The <see cref="ValidationBehavior"/> allows users to create custom validation behaviors. All of the validation behaviors in the Xamarin Community Toolkit inherit from this behavior, to expose a number of shared properties. Users can inherit from this class to create a custom validation behavior currently not supported through the Xamarin Community Toolkit. This behavios cannot be used directly as it's abstract.
/// </summary>
public abstract class ValidationBehavior : BaseValidationBehavior
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="ValuePropertyName"/> property.
	/// </summary>
	public static readonly BindableProperty ValuePropertyNameProperty =
		BindableProperty.Create(nameof(ValuePropertyName), typeof(string), typeof(ValidationBehavior), defaultValueCreator: GetDefaultValuePropertyName, propertyChanged: OnValuePropertyNamePropertyChanged);

	/// <summary>
	/// Initialize a new instance of ValidationBehavior
	/// </summary>
	public ValidationBehavior() : base() { }

	/// <summary>
	/// Allows the user to override the property that will be used as the value to validate. This is a bindable property.
	/// </summary>
	public string? ValuePropertyName
	{
		get => (string?)GetValue(ValuePropertyNameProperty);
		set => SetValue(ValuePropertyNameProperty, value);
	}
	/// <summary>
	/// Default value property name
	/// </summary>
	protected virtual string DefaultValuePropertyName { get; } = Entry.TextProperty.PropertyName;

	static void OnValuePropertyNamePropertyChanged(BindableObject bindable, object oldValue, object newValue)
		=> ((ValidationBehavior)bindable).OnValuePropertyNamePropertyChanged();


	static object GetDefaultValuePropertyName(BindableObject bindable)
		=> ((ValidationBehavior)bindable).DefaultValuePropertyName;

	///<inheritdoc/>
	protected override BindingBase CreateBinding()
	{
		return new Binding
		{
			Path = ValuePropertyName,
			Source = View
		};
	}

	void OnValuePropertyNamePropertyChanged()
	{
		base.ConfigureValueBinding();
	}
}

/// <inheritdoc />
public abstract class ValidationBehavior<T> : ValidationBehavior
{
	/// <summary>
	/// The value to validate. This is a bindable property.
	/// </summary>
	public new T? Value
	{
		get => (T?)GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}

	/// <summary>
	/// Decorate value
	/// </summary>
	protected virtual T? Decorate(T? value) => value;

	/// <inheritdoc />
	protected override object? Decorate(object? value) => (T?)value;

	/// <summary>
	/// Validate value
	/// </summary>
	protected abstract ValueTask<bool> ValidateAsync(T? value, CancellationToken token);

	/// <inheritdoc />
	protected override ValueTask<bool> ValidateAsync(object? value, CancellationToken token) => ValidateAsync((T?)value, token);
}