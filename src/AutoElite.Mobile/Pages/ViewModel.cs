using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoElite.Mobile.Pages;

/// <summary>The state of a page that its controls bind to, which tells them when a property changes.</summary>
public abstract class ViewModel : INotifyPropertyChanged
{
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Sets a property and tells the controls, unless the value is the same.</summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="field">The field of the property.</param>
    /// <param name="value">The new value.</param>
    /// <param name="propertyName">The name of the property, given by the compiler.</param>
    /// <returns><see langword="true"/> when the value changed.</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);

        return true;
    }

    /// <summary>Tells the controls that a property changed.</summary>
    /// <param name="propertyName">The name of the property.</param>
    protected void OnPropertyChanged(string? propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
