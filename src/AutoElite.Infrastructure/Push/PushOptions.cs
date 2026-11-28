namespace AutoElite.Infrastructure.Push;

/// <summary>
/// How push notifications are sent, from the <see cref="SectionName"/> configuration section. Without a service account
/// no push notification is sent and the notifications stay in the applications. The file of the service account is a
/// secret: it is kept outside the repository, and its path is set with user secrets locally and with an environment
/// variable when deployed (<c>Push__ServiceAccountFile</c>), with the file mounted as a secret.
/// </summary>
public sealed class PushOptions
{
    /// <summary>The configuration section of the settings.</summary>
    public const string SectionName = "Push";

    /// <summary>
    /// Gets or sets the path of the JSON key of a service account of the Firebase project, which may send messages
    /// through Firebase Cloud Messaging; push notifications are off when it is not set.
    /// </summary>
    public string? ServiceAccountFile { get; set; }

    /// <summary>Gets a value indicating whether push notifications are sent.</summary>
    internal bool IsEnabled => !string.IsNullOrWhiteSpace(ServiceAccountFile);

    /// <summary>Lists what is wrong with the settings, for the validation when the host starts.</summary>
    /// <returns>The problems, if any.</returns>
    internal IEnumerable<string> Problems()
    {
        if (!IsEnabled)
            yield break;

        if (!File.Exists(ServiceAccountFile))
        {
            yield return $"{SectionName}:ServiceAccountFile names no file. Set the path of the JSON key of the Firebase service "
                + $"account with dotnet user-secrets locally or the {SectionName}__ServiceAccountFile environment variable.";
            yield break;
        }

        foreach (var problem in FirebaseServiceAccount.ProblemsOf(File.ReadAllText(ServiceAccountFile)))
            yield return $"{SectionName}:ServiceAccountFile: {problem}";
    }
}
