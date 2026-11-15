using AutoElite.Client.Api.Models;

namespace AutoElite.Mobile.Pages;

/// <summary>The names of the licence categories as the licence prints them, such as "BE" or "Tm".</summary>
internal static class CategoryNames
{
    /// <summary>Gives the name of a category.</summary>
    /// <param name="category">The category.</param>
    /// <returns>The name.</returns>
    public static string Of(LicenseCategory category) => category.ToString();
}
