using Microsoft.Maui.Controls.Shapes;

namespace AutoElite.Mobile.Controls;

/// <summary>
/// The icons of Noir Modern by their names: outlines on a grid of 24, drawn after Lucide (design/icons), so that every
/// screen speaks one visual language. <see cref="AppIcon"/> draws them.
/// </summary>
public static class AppIcons
{
    private static readonly PathGeometryConverter _converter = new();

    private static readonly Dictionary<string, string> _paths = new(StringComparer.Ordinal)
    {
        ["bell"] = "M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9 M10.3 21a1.94 1.94 0 0 0 3.4 0",
        ["book-open"] = "M2 5h6a4 4 0 0 1 4 4v11a3 3 0 0 0-3-3H2z M22 5h-6a4 4 0 0 0-4 4v11a3 3 0 0 1 3-3h7z",
        ["calendar"] = "M5 5h14a2 2 0 0 1 2 2v12a2 2 0 0 1 -2 2h-14a2 2 0 0 1 -2 -2v-12a2 2 0 0 1 2 -2z M3 10h18 M8 3v4 M16 3v4",
        ["camera"] =
            "M14.5 4h-5L7 7H4a2 2 0 0 0-2 2v9a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V9a2 2 0 0 0-2-2h-3z M9 13a3 3 0 1 0 6 0a3 3 0 " +
            "1 0 -6 0",
        ["car"] = "M19 17h2v-5l-2.5-5h-13L3 12v5h2 M9 17h6 M5 17a2 2 0 1 0 4 0a2 2 0 1 0 -4 0 M15 17a2 2 0 1 0 4 0a2 2 0 1 0 -4 0",
        ["chart-column"] = "M3 3v18h18 M7 16v-5 M12 16V8 M17 16v-8",
        ["check"] = "M20 6 9 17l-5-5",
        ["chevron-left"] = "M15 18l-6-6 6-6",
        ["chevron-right"] = "M9 18l6-6-6-6",
        ["circle-x"] = "M3 12a9 9 0 1 0 18 0a9 9 0 1 0 -18 0 M15 9l-6 6 M9 9l6 6",
        ["clipboard-check"] =
            "M9 2h6a1 1 0 0 1 1 1v2a1 1 0 0 1 -1 1h-6a1 1 0 0 1 -1 -1v-2a1 1 0 0 1 1 -1z M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 " +
            "1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2 M9 14l2 2 4-4",
        ["clock"] = "M3 12a9 9 0 1 0 18 0a9 9 0 1 0 -18 0 M12 7v5l3 2",
        ["ellipsis"] =
            "M4.4 12a0.6 0.6 0 1 0 1.2 0a0.6 0.6 0 1 0 -1.2 0 M11.4 12a0.6 0.6 0 1 0 1.2 0a0.6 0.6 0 1 0 -1.2 0 M18.4 " +
            "12a0.6 0.6 0 1 0 1.2 0a0.6 0.6 0 1 0 -1.2 0",
        ["external-link"] = "M15 3h6v6 M10 14 21 3 M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6",
        ["file-text"] = "M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z M14 2v6h6 M8 13h8 M8 17h5",
        ["globe"] =
            "M2 12a10 10 0 1 0 20 0a10 10 0 1 0 -20 0 M2 12h20 M12 2a15 15 0 0 1 4 10 15 15 0 0 1-4 10 15 15 0 0 1-4-10 15 " +
            "15 0 0 1 4-10z",
        ["home"] = "M3 10.5 12 3l9 7.5V20a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1z",
        ["inbox"] =
            "M22 12h-6l-2 3h-4l-2-3H2 M5.45 5.11 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 " +
            "4H7.24a2 2 0 0 0-1.79 1.11z",
        ["info"] = "M3 12a9 9 0 1 0 18 0a9 9 0 1 0 -18 0 M12 16v-4 M12 8h.01",
        ["log-out"] = "M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4 M16 17l5-5-5-5 M21 12H9",
        ["mail"] = "M4 4h16a2 2 0 0 1 2 2v12a2 2 0 0 1 -2 2h-16a2 2 0 0 1 -2 -2v-12a2 2 0 0 1 2 -2z M22 7l-10 6L2 7",
        ["map-pin"] = "M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0z M9 10a3 3 0 1 0 6 0a3 3 0 1 0 -6 0",
        ["message-circle"] = "M21 12a8 8 0 0 1-11.6 7.1L4 20l1-4.6A8 8 0 1 1 21 12z",
        ["navigation"] = "M3 11 22 2l-9 19-2-8z",
        ["paperclip"] = "M21.4 11.1l-9.2 9.2a6 6 0 0 1-8.5-8.5l9.2-9.2a4 4 0 0 1 5.7 5.7l-9.2 9.2a2 2 0 0 1-2.8-2.8l8.5-8.5",
        ["pencil"] = "M12 20h9 M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4z",
        ["phone"] =
            "M22 16.9v3a2 2 0 0 1-2.2 2 19.8 19.8 0 0 1-8.6-3.1 19.5 19.5 0 0 1-6-6A19.8 19.8 0 0 1 2.1 4.2 2 2 0 0 1 4.1 " +
            "2h3a2 2 0 0 1 2 1.7c.1.9.4 1.8.7 2.7a2 2 0 0 1-.5 2.1L8 9.8a16 16 0 0 0 6 6l1.3-1.3a2 2 0 0 1 2.1-.4c.9.3 " +
            "1.8.6 2.7.7a2 2 0 0 1 1.7 2z",
        ["plus"] = "M12 5v14 M5 12h14",
        ["search"] = "M4 11a7 7 0 1 0 14 0a7 7 0 1 0 -14 0 M21 21l-4.3-4.3",
        ["send"] = "M22 2 11 13 M22 2l-7 20-4-9-9-4z",
        ["shield"] = "M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z",
        ["sun"] =
            "M8 12a4 4 0 1 0 8 0a4 4 0 1 0 -8 0 M12 2v2 M12 20v2 M4.9 4.9l1.4 1.4 M17.7 17.7l1.4 1.4 M2 12h2 M20 12h2 M4.9 " +
            "19.1l1.4-1.4 M17.7 6.3l1.4-1.4",
        ["triangle-alert"] = "M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0z M12 9v4 M12 17h.01",
        ["user"] = "M8 8a4 4 0 1 0 8 0a4 4 0 1 0 -8 0 M4 21a8 8 0 0 1 16 0",
        ["users"] = "M5 8a4 4 0 1 0 8 0a4 4 0 1 0 -8 0 M1 21a8 8 0 0 1 16 0 M16 4.1a4 4 0 0 1 0 7.8 M19 14.5a8 8 0 0 1 4 6.5",
        ["wallet"] = "M19 7V5a2 2 0 0 0-2-2H5a2 2 0 0 0 0 4h14a1 1 0 0 1 1 1v12a1 1 0 0 1-1 1H5a2 2 0 0 1-2-2V5 M16 14h.01",
        ["x"] = "M18 6 6 18 M6 6l12 12"
    };

    /// <summary>Gets the names of the icons.</summary>
    public static IReadOnlyCollection<string> Names => _paths.Keys;

    /// <summary>Makes the outline of an icon, a new one for each shape that draws it.</summary>
    /// <param name="name">The name of the icon, such as calendar.</param>
    /// <returns>The outline, or <see langword="null"/> for an unknown name.</returns>
    public static Geometry? GeometryOf(string? name) =>
        name is not null && _paths.TryGetValue(name, out var data) ? (Geometry?)_converter.ConvertFromInvariantString(data) : null;
}
