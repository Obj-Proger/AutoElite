namespace AutoElite.Mobile.Controls;

/// <summary>
/// Shows which field has the focus: the border around an entry, an editor or a picker (the Field style) turns to the
/// accent while the control inside has the focus. The native frames of the controls are removed
/// (<see cref="NativeFields"/>), so that this border is the only one; the styles of the controls turn it on.
/// </summary>
public static class FieldFocus
{
    /// <summary>Identifies the attached property that turns the highlight on for a control.</summary>
    public static readonly BindableProperty IsEnabledProperty = BindableProperty.CreateAttached(
        "IsEnabled", typeof(bool), typeof(FieldFocus), false, propertyChanged: OnIsEnabledChanged);

    /// <summary>Tells whether the highlight is on for a control.</summary>
    /// <param name="view">The control.</param>
    /// <returns><see langword="true"/> when it is on.</returns>
    public static bool GetIsEnabled(BindableObject view)
    {
        ArgumentNullException.ThrowIfNull(view);

        return (bool)view.GetValue(IsEnabledProperty);
    }

    /// <summary>Turns the highlight on or off for a control.</summary>
    /// <param name="view">The control.</param>
    /// <param name="value">Whether it is on.</param>
    public static void SetIsEnabled(BindableObject view, bool value)
    {
        ArgumentNullException.ThrowIfNull(view);
        view.SetValue(IsEnabledProperty, value);
    }

    private static void OnIsEnabledChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not VisualElement view)
            return;

        view.Focused -= OnFocusChanged;
        view.Unfocused -= OnFocusChanged;
        if (newValue is true)
        {
            view.Focused += OnFocusChanged;
            view.Unfocused += OnFocusChanged;
        }
    }

    private static void OnFocusChanged(object? sender, FocusEventArgs e)
    {
        if (sender is VisualElement { Parent: Border field }
            && Application.Current?.Resources.TryGetValue(e.IsFocused ? "Accent" : "Border", out var color) == true
            && color is Color stroke)
        {
            field.Stroke = stroke;
        }
    }
}
