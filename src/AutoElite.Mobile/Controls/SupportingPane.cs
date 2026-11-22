using Microsoft.Maui.Layouts;

namespace AutoElite.Mobile.Controls;

/// <summary>
/// The main content of a page with content that supports it, such as today's lesson with the lessons ahead: side by
/// side from <see cref="Threshold"/> wide, the supporting content on the right in about a third of the width, and one
/// under the other on a narrower window. Its first child is the main content, its second the supporting one.
/// </summary>
public sealed class SupportingPane : Layout
{
    /// <summary>Identifies the <see cref="Threshold"/> property.</summary>
    public static readonly BindableProperty ThresholdProperty = BindableProperty.Create(
        nameof(Threshold), typeof(double), typeof(SupportingPane), 900d, propertyChanged: OnLayoutChanged);

    /// <summary>Identifies the <see cref="Spacing"/> property.</summary>
    public static readonly BindableProperty SpacingProperty = BindableProperty.Create(
        nameof(Spacing), typeof(double), typeof(SupportingPane), 24d, propertyChanged: OnLayoutChanged);

    // The share of the width the supporting content takes beside the main one, and its narrowest and widest.
    private const double SideShare = 0.4;
    private const double SideMin = 320;
    private const double SideMax = 480;

    /// <summary>Gets or sets the width from which the two stand side by side.</summary>
    public double Threshold
    {
        get => (double)GetValue(ThresholdProperty);
        set => SetValue(ThresholdProperty, value);
    }

    /// <summary>Gets or sets the space between the two.</summary>
    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    /// <inheritdoc />
    protected override ILayoutManager CreateLayoutManager() => new Manager(this);

    private static void OnLayoutChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((IView)bindable).InvalidateMeasure();

    private sealed class Manager(SupportingPane pane) : LayoutManager(pane)
    {
        public override Size Measure(double widthConstraint, double heightConstraint)
        {
            var padding = pane.Padding;
            var width = double.IsFinite(widthConstraint) ? Math.Max(0, widthConstraint - padding.HorizontalThickness) : SideMax;
            var (main, side) = Parts();
            double height;
            if (IsSideBySide(width))
            {
                var (mainWidth, sideWidth) = WidthsOf(width);
                height = Math.Max(
                    main?.Measure(mainWidth, double.PositiveInfinity).Height ?? 0,
                    side?.Measure(sideWidth, double.PositiveInfinity).Height ?? 0);
            }
            else
            {
                var mainHeight = main?.Measure(width, double.PositiveInfinity).Height ?? 0;
                var sideHeight = side?.Measure(width, double.PositiveInfinity).Height ?? 0;
                height = mainHeight + sideHeight + (mainHeight > 0 && sideHeight > 0 ? pane.Spacing : 0);
            }

            return new Size(width + padding.HorizontalThickness, height + padding.VerticalThickness);
        }

        public override Size ArrangeChildren(Rect bounds)
        {
            var padding = pane.Padding;
            var left = bounds.Left + padding.Left;
            var top = bounds.Top + padding.Top;
            var width = Math.Max(0, bounds.Width - padding.HorizontalThickness);
            var (main, side) = Parts();
            if (IsSideBySide(width))
            {
                var (mainWidth, sideWidth) = WidthsOf(width);
                main?.Arrange(new Rect(left, top, mainWidth, main.DesiredSize.Height));
                side?.Arrange(new Rect(left + mainWidth + pane.Spacing, top, sideWidth, side.DesiredSize.Height));
            }
            else
            {
                var mainHeight = main?.DesiredSize.Height ?? 0;
                main?.Arrange(new Rect(left, top, width, mainHeight));
                side?.Arrange(new Rect(left, top + mainHeight + (mainHeight > 0 ? pane.Spacing : 0), width, side.DesiredSize.Height));
            }

            return bounds.Size;
        }

        private bool IsSideBySide(double width) => width >= pane.Threshold;

        private (double Main, double Side) WidthsOf(double width)
        {
            var side = Math.Clamp(width * SideShare, SideMin, SideMax);

            return (Math.Max(0, width - side - pane.Spacing), side);
        }

        private (IView? Main, IView? Side) Parts()
        {
            var visible = pane.Where(child => child.Visibility != Visibility.Collapsed).ToList();

            return (visible.ElementAtOrDefault(0), visible.ElementAtOrDefault(1));
        }
    }
}
