using Microsoft.Extensions.Options;

namespace AutoElite.Infrastructure.Authentication;

/// <summary>Validates settings with their own list of problems, so that a misconfigured host fails with every reason.</summary>
/// <typeparam name="TOptions">The type of the settings.</typeparam>
internal sealed class SettingsValidator<TOptions>(Func<TOptions, IEnumerable<string>> problems) : IValidateOptions<TOptions>
    where TOptions : class
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, TOptions options)
    {
        string[] found = [.. problems(options)];

        return found.Length == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(found);
    }
}
