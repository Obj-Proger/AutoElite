using Microsoft.Maui.Layouts;

namespace AutoElite.Mobile.Controls;

/// <summary>
/// Cards of the same kind, such as the leads or the contracts, in as many columns of equal width as fit, each at least
/// <see cref="MinItemWidth"/> wide and at most <see cref="MaxColumns"/>: one column on a phone, a grid on a wide window.
/// A row is as tall as its tallest card.
/// </summary>
public sealed class AdaptiveGrid : Layout
{
    /// <summary>Identifies the <see cref="MinItemWidth"/> property.</summary>
    public static readonly BindableProperty MinItemWidthProperty = BindableProperty.Create(
        nameof(MinItemWidth), typeof(double), typeof(AdaptiveGrid), 340d, propertyChanged: OnLayoutChanged);

    /// <summary>Identifies the <see cref="MaxColumns"/> property.</summary>
    public static readonly BindableProperty MaxColumnsProperty = BindableProperty.Create(
        nameof(MaxColumns), typeof(int), typeof(AdaptiveGrid), 3, propertyChanged: OnLayoutChanged);

    /// <summary>Identifies the <see cref="Spacing"/> property.</summary>
    public static readonly BindableProperty SpacingProperty = BindableProperty.Create(
        nameof(Spacing), typeof(double), typeof(AdaptiveGrid), 10d, propertyChanged: OnLayoutChanged);

    /// <summary>Gets or sets the narrowest a card may be before the grid takes a column fewer.</summary>
    public double MinItemWidth
    {
        get => (double)GetValue(MinItemWidthProperty);
        set => SetValue(MinItemWidthProperty, value);
    }

    /// <summary>Gets or sets the most columns the grid takes, however wide the window.</summary>
    public int MaxColumns
    {
        get => (int)GetValue(MaxColumnsProperty);
        set => SetValue(MaxColumnsProperty, value);
    }

    /// <summary>Gets or sets the space between the cards, across and down.</summary>
    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    /// <inheritdoc />
    protected override ILayoutManager CreateLayoutManager() => new Manager(this);

    private static void OnLayoutChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((IView)bindable).InvalidateMeasure();

    private sealed class Manager(AdaptiveGrid grid) : LayoutManager(grid)
    {
        public override Size Measure(double widthConstraint, double heightConstraint)
        {
            var padding = grid.Padding;
            var width = double.IsFinite(widthConstraint)
                ? Math.Max(0, widthConstraint - padding.HorizontalThickness)
                : grid.MinItemWidth;
            var (columns, columnWidth) = ColumnsOf(width);
            var height = 0d;
            var rows = 0;
            foreach (var row in Rows(columns))
            {
                height += row.Max(child => child.Measure(columnWidth, double.PositiveInfinity).Height);
                rows++;
            }

            height += Math.Max(0, rows - 1) * grid.Spacing;

            return new Size(width + padding.HorizontalThickness, height + padding.VerticalThickness);
        }

        public override Size ArrangeChildren(Rect bounds)
        {
            var padding = grid.Padding;
            var width = Math.Max(0, bounds.Width - padding.HorizontalThickness);
            var (columns, columnWidth) = ColumnsOf(width);
            var top = bounds.Top + padding.Top;
            foreach (var row in Rows(columns))
            {
                var height = row.Max(child => child.DesiredSize.Height);
                for (var index = 0; index < row.Count; index++)
                {
                    var left = bounds.Left + padding.Left + (index * (columnWidth + grid.Spacing));
                    row[index].Arrange(new Rect(left, top, columnWidth, height));
                }

                top += height + grid.Spacing;
            }

            return bounds.Size;
        }

        private (int Columns, double Width) ColumnsOf(double width)
        {
            var spacing = grid.Spacing;
            var columns = (int)Math.Floor((width + spacing) / (Math.Max(1, grid.MinItemWidth) + spacing));
            columns = Math.Clamp(columns, 1, Math.Max(1, grid.MaxColumns));

            return (columns, Math.Max(0, (width - ((columns - 1) * spacing)) / columns));
        }

        private IEnumerable<List<IView>> Rows(int columns) =>
            grid.Where(child => child.Visibility != Visibility.Collapsed).Chunk(columns).Select(row => row.ToList());
    }
}
