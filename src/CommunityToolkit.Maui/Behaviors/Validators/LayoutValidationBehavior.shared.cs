using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Converters;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The <see cref="LayoutValidationBehavior"/> allows users to create custom validation behaviors. All of the validation behaviors in the Xamarin Community Toolkit inherit from this behavior, to expose a number of shared properties. Users can inherit from this class to create a custom validation behavior currently not supported through the Xamarin Community Toolkit. This behavios cannot be used directly as it's abstract.
/// </summary>
public class LayoutValidationBehavior : BaseValidationBehavior
{
	/// <summary>
	/// Default VariableMultiValueConverter
	/// </summary>
	protected virtual VariableMultiValueConverter DefaultVariableMultiValueConverter { get; }
	/// <summary>
	/// Backing BindableProperty for the <see cref="VariableMultiValueConverter"/> property.
	/// </summary>
	public static readonly BindableProperty VariableMultiValueConverterProperty =
		BindableProperty.Create(nameof(VariableMultiValueConverter), typeof(VariableMultiValueConverter), typeof(LayoutValidationBehavior), defaultValueCreator: CreateDefaultConverter);
	/// <summary>
	/// Converter in charge of converting all the IsValid from the individual behaviors to a single boolean that shows in the IsValid.
	/// </summary>
	public VariableMultiValueConverter VariableMultiValueConverter
	{
		get => (VariableMultiValueConverter)GetValue(VariableMultiValueConverterProperty);
		set => SetValue(VariableMultiValueConverterProperty, value);
	}
	/// <summary>
	/// List containing all the ValidationBehaviors attached to elements in the layout.
	/// </summary>
	public List<BaseValidationBehavior> Behaviors { get; set; }
	/// <summary>
	/// Initialize a new instance of ValidationBehavior
	/// </summary>
	public LayoutValidationBehavior() : base() 
	{
		Behaviors = new List<BaseValidationBehavior>();
		DefaultVariableMultiValueConverter = new VariableMultiValueConverter { ConditionType = MultiBindingCondition.All };
		if (View != null)
		{
			View.DescendantAdded += OnDescendantAdded;
		}
	}
	static object CreateDefaultConverter(BindableObject bindable) => ((LayoutValidationBehavior)bindable).DefaultVariableMultiValueConverter;

	///<inheritdoc/>
	protected override BindingBase CreateBinding()
	{
		var bindings = new List<BindingBase>();
		foreach(var behavior in Behaviors)
		{
			bindings.Add(new Binding
			{
				Source = behavior,
				Path = nameof(behavior.IsValid)
			});
		}
		return new MultiBinding
		{
			Bindings = bindings,
			Converter = VariableMultiValueConverter
		};
	}
	void OnDescendantAdded(object? sender, ElementEventArgs e)
	{
		if(e.Element is Entry entry)
		{
			var validations = entry.Behaviors.Where(b => b is BaseValidationBehavior).Select(b=> (BaseValidationBehavior)b).ToList();
			Behaviors.AddRange(validations);
		}
	}
	///<inheritdoc/>
	protected override ValueTask<bool> ValidateAsync(object? value, CancellationToken token)
	{
		if(value is bool convertedResult)
		{
			return new ValueTask<bool>(convertedResult);
		}
		return new ValueTask<bool>(false);
	}
}