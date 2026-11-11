using Microsoft.Extensions.Options;

namespace AutoElite.API.Hosting;

/// <summary>Validates settings of the host by the problems they tell, so that the host does not start with wrong ones.</summary>
/// <param name="problems">Tells what is wrong with the settings; nothing when they are valid.</param>
/// <typeparam name="TSettings">The type of the settings.</typeparam>
internal sealed class SettingsValidator<TSettings>(Func<TSettings, IEnumerable<string>> problems) : IValidateOptions<TSettings>
    where TSettings : class
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, TSettings options)
    {
        var found = problems(options).ToList();

        return found.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(found);
    }
}
