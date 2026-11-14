#if ANDROID
using Android.Content.Res;
using Microsoft.Maui.Handlers;
#endif

namespace AutoElite.Mobile.Controls;

/// <summary>
/// Removes the frames that the platforms draw around entries, editors and pickers, since the pages draw their own
/// border around a field (the Field style, with <see cref="FieldFocus"/>): the underline of Android here, the frame and
/// background of the text box and combo box of Windows in Platforms/Windows/App.xaml.
/// </summary>
internal static class NativeFields
{
    /// <summary>Changes the handlers of the controls; called once, when the application is built.</summary>
    public static void RemoveFrames()
    {
#if ANDROID
        EntryHandler.Mapper.AppendToMapping(nameof(NativeFields), (handler, _) => RemoveUnderline(handler.PlatformView));
        EditorHandler.Mapper.AppendToMapping(nameof(NativeFields), (handler, _) => RemoveUnderline(handler.PlatformView));
        PickerHandler.Mapper.AppendToMapping(nameof(NativeFields), (handler, _) => RemoveUnderline(handler.PlatformView));
#endif
    }

#if ANDROID
    private static void RemoveUnderline(Android.Widget.EditText view) =>
        view.BackgroundTintList = ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
}
