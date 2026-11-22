using Microsoft.Maui.Layouts;

namespace AutoElite.Mobile.Controls;

/// <summary>
/// The content of a page as a column that starts under the title of the page and takes the width of the window up to
/// its <see cref="VisualElement.MaximumWidthRequest"/>, so that on a wide window its lines stay readable without the
/// column floating in the middle; on a phone it takes the whole width.
/// </summary>
public sealed class PageColumn : VerticalStackLayout
{
    /// <summary>Initializes a new instance of the <see cref="PageColumn"/> class.</summary>
    public PageColumn() => HorizontalOptions = LayoutOptions.Start;

    /// <inheritdoc />
    protected override ILayoutManager CreateLayoutManager() => new Manager(this);

    // A stack that asks for the whole width it is given, so that aligning it to the start does not shrink it to its text.
    private sealed class Manager(PageColumn column) : VerticalStackLayoutManager(column)
    {
        public override Size Measure(double widthConstraint, double heightConstraint)
        {
            var size = base.Measure(widthConstraint, heightConstraint);

            return double.IsFinite(widthConstraint) ? new Size(widthConstraint, size.Height) : size;
        }
    }
}
