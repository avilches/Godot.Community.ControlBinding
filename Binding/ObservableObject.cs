using ControlBinding.Collections;
using ControlBinding.ControlBinders;
using ControlBinding.Formatters;
using ControlBinding.Interfaces;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ControlBinding;

public partial class ObservableObject : Node, IObservableObject
{
    private readonly List<Binding> _controlBindings = new List<Binding>();
    private ControlBinderProvider _controlBinderProvider = new ControlBinderProvider();
    private readonly object cleanUpLock = 0;

    [Signal]
    public delegate void PropertyChangedEventHandler(GodotObject owner, string propertyName);

    public override void _Ready()
    {
        base._Ready();
    }
    /// <summary>
    /// Raise OnPropertyChanged when a bound property on this object changes
    /// </summary>
    /// <param name="name"></param>
    public void OnPropertyChanged([CallerMemberName] string name = "not a property")
    {
        if(name == "not a property")
            return;
            
        EmitSignal(SignalName.PropertyChanged, this, name);

        var invalidBindings = _controlBindings.Where(x => x.BindingStatus == BindingStatus.Invalid);
        if (invalidBindings.Any())
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                lock (cleanUpLock)
                {
                    var invalidBindings = _controlBindings.Where(x => x.BindingStatus == BindingStatus.Invalid).ToList();
                    foreach (var binding in invalidBindings)
                    {
                        _controlBindings.Remove(binding);
                    }
                }
            });
        }
    }

    /// <summary>
    /// Sets a value to the backing field of a property and triggers <see cref="OnPropertyChanged"/>
    /// </summary>
    /// <param name="field">The backing field of the property</param>
    /// <param name="value">The value that should be set</param>
    /// <param name="name">Name of the property</param>
    /// <typeparam name="T">Type of the property</typeparam>
    public void SetValue<T>(ref T field, T value, [CallerMemberName] string name = "not a property")
    {
        field = value;
        OnPropertyChanged(name);
    }

    /// <summary>
    /// Bind a control property to an object property
    /// </summary>
    /// <param name="controlPath">The path of the Godot control in the scene</param>
    /// <param name="sourceProperty">The property of the Godot control to bind to</param>
    /// <param name="path">The path of the property to bind to. Relative to this object</param>
    /// <param name="bindingMode">The binding mode to use</param>
    /// <param name="formatter">The <see cref="ControlBinding.Formatters.IValueFormatter" /> to use to format the the Control property and target property</param>
    public void BindProperty(
        string controlPath,
        string sourceProperty,
        string path,
        BindingMode bindingMode = BindingMode.OneWay,
        IValueFormatter formatter = null
        )
    {
        var node = GetNode<Godot.Control>(controlPath);
        if (node == null)
        {
            GD.PrintErr($"DataBinding: Unable to find node with path '{controlPath}'");
            return;
        }

        if (_controlBinderProvider.GetBinder(node) is IControlBinder binder)
        {
            var bindingConfiguration = new BindingConfiguration
            {
                BindingMode = bindingMode,
                BoundPropertyName = sourceProperty,
                Path = path,
                BoundControl = new WeakReference(node),
                Formatter = formatter,
                Owner = this,
            };

            var binding = new Binding(bindingConfiguration, binder);
            binding.BindControl();
            _controlBindings.Add(binding);
        }
    }

    /// <summary>
    /// Bind a list control to an IObservableList or IList property
    /// Note: list controls include OptionButton and ItemList
    /// </summary>
    /// <param name="controlPath">The path of the Godot control in the scene.</param>
    /// <param name="path">The path of the property to bind to. Relative to this object.</param>
    /// <param name="bindingMode">The binding mode to use</param>
    /// <param name="formatter">The IValueFormatter to use to format the the list item and target property. Return a <see cref="ControlBinding.Collections.ListItem"/> for greater formatting control.</param>
    public void BindListProperty(
        string controlPath,
        string path,
        BindingMode bindingMode = BindingMode.OneWay,
        IValueFormatter formatter = null)
    {
        var node = GetNode<Godot.Control>(controlPath);
        if (node == null)
        {
            GD.PrintErr($"DataBinding: Unable to find node with path '{controlPath}'");
            return;
        }

        if (_controlBinderProvider.GetBinder(node) is IControlBinder binder)
        {
            var bindingConfiguration = new BindingConfiguration
            {
                BindingMode = bindingMode,
                BoundControl = new WeakReference(node),
                Formatter = formatter,
                IsListBinding = true,
                Owner = this,
                Path = path
            };

            var binding = new Binding(bindingConfiguration, binder);
            binding.BindControl();
            _controlBindings.Add(binding);
        }
    }

    /// <summary>
    /// Binds an emum to an OptionButton control with optional path for the selected value
    /// </summary>
    /// <param name="controlPath">The path of the Godot control in the scene.</param>
    /// <param name="selectedItemPath">The path of the property to bind to. Relative to this object.</param>
    /// <typeparam name="T">The enum type to bind the OptionButton to</typeparam>
    public void BindEnumProperty<T>(string controlPath, string selectedItemPath = null) where T : Enum
    {
        var node = GetNode<Godot.Control>(controlPath);
        if (node == null)
        {
            GD.PrintErr($"DataBinding: Unable to find node with path '{controlPath}'");
            return;
        }

        if (node is not OptionButton)
        {
            GD.PrintErr($"DataBinding: Enum property binding must be backed by an OptionButton");
            return;
        }

        ObservableList<T> targetObject = new ObservableList<T>();
        foreach (var entry in Enum.GetValues(typeof(T)))
        {
            targetObject.Add((T)entry);
        }

        // bind the list items (static list binding - enums won't change at runtime)
        if (_controlBinderProvider.GetBinder(node) is IControlBinder binder)
        {
            var bindingConfiguration = new BindingConfiguration
            {
                BindingMode = BindingMode.OneWay,
                TargetObject = targetObject,
                BoundControl = new WeakReference(node),
                IsListBinding = true,
                Path = string.Empty,
                Formatter = new ValueFormatter
                {
                    FormatControl = (v) =>
                    {
                        var enumValue = (T)v;
                        return new ListItem
                        {
                            DisplayValue = enumValue.ToString(),
                            Id = (int)Enum.Parse(typeof(T), v.ToString())
                        };
                    }
                }
            };

            var binding = new Binding(bindingConfiguration, binder);
            binding.BindControl();
            _controlBindings.Add(binding);
        }
        if (!string.IsNullOrEmpty(selectedItemPath))
        {
            BindProperty(controlPath, "Selected", selectedItemPath, BindingMode.TwoWay, new ValueFormatter
            {
                FormatTarget = (v) =>
                {
                    return targetObject[(int)v == -1 ? 0 : (int)v];
                },
                FormatControl = (v) =>
                {
                    return targetObject.IndexOf((T)v);
                }
            });
        }
    }

    public void BindSceneListProperty(string controlPath, string path, ISceneFormatter sceneFormatter)
    {

    }
}
