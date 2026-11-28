using System.Security.Cryptography;
using System.Text.Json;

namespace AutoElite.Infrastructure.Push;

/// <summary>
/// The parts of the JSON key of a Firebase service account that sending needs: the project, the account, its private
/// key and the address that exchanges a signed assertion for an access token, which must be Google's over HTTPS.
/// </summary>
/// <remarks>A class rather than a record, so that no generated text of it ever shows the private key.</remarks>
internal sealed class FirebaseServiceAccount
{
    private const string GoogleTokenHost = "oauth2.googleapis.com";

    /// <summary>Initializes a new instance of the <see cref="FirebaseServiceAccount"/> class.</summary>
    /// <param name="projectId">The Firebase project, whose devices are reached.</param>
    /// <param name="clientEmail">The email address of the service account.</param>
    /// <param name="privateKey">The private RSA key of the service account, in PEM.</param>
    /// <param name="tokenUri">The address of Google's token endpoint.</param>
    public FirebaseServiceAccount(string projectId, string clientEmail, string privateKey, Uri tokenUri)
    {
        ProjectId = projectId;
        ClientEmail = clientEmail;
        PrivateKey = privateKey;
        TokenUri = tokenUri;
    }

    /// <summary>Gets the Firebase project, whose devices are reached.</summary>
    public string ProjectId { get; }

    /// <summary>Gets the email address of the service account.</summary>
    public string ClientEmail { get; }

    /// <summary>Gets the private RSA key of the service account, in PEM; a secret.</summary>
    public string PrivateKey { get; }

    /// <summary>Gets the address of Google's token endpoint.</summary>
    public Uri TokenUri { get; }

    /// <summary>Reads a service account from its JSON key.</summary>
    /// <param name="json">The content of the JSON key.</param>
    /// <returns>The service account.</returns>
    /// <exception cref="InvalidOperationException">The key is not a usable service account.</exception>
    public static FirebaseServiceAccount Parse(string json)
    {
        var problem = ProblemsOf(json).FirstOrDefault();
        if (problem is not null)
            throw new InvalidOperationException(problem);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        return new FirebaseServiceAccount(
            root.GetProperty("project_id").GetString()!,
            root.GetProperty("client_email").GetString()!,
            root.GetProperty("private_key").GetString()!,
            TokenUriOf(root));
    }

    /// <summary>Lists what makes a JSON key unusable, without telling any of its secrets.</summary>
    /// <param name="json">The content of the JSON key.</param>
    /// <returns>The problems, if any.</returns>
    public static IEnumerable<string> ProblemsOf(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return ["the file is not JSON."];
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return ["the file is not a JSON key of a service account."];

            List<string> problems = [];
            foreach (var name in new[] { "project_id", "client_email", "private_key" })
            {
                if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(value.GetString()))
                {
                    problems.Add($"\"{name}\" is missing.");
                }
            }

            if (problems.Count == 0 && !IsRsaKey(root.GetProperty("private_key").GetString()!))
                problems.Add("\"private_key\" is not an RSA private key in PEM.");

            var tokenUri = TokenUriOf(root);
            if (tokenUri.Scheme != Uri.UriSchemeHttps || !string.Equals(tokenUri.Host, GoogleTokenHost, StringComparison.OrdinalIgnoreCase))
                problems.Add($"\"token_uri\" must be https://{GoogleTokenHost}/token.");

            return problems;
        }
    }

    private static Uri TokenUriOf(JsonElement root) =>
        root.TryGetProperty("token_uri", out var value) && value.ValueKind == JsonValueKind.String
            && Uri.TryCreate(value.GetString(), UriKind.Absolute, out var uri)
            ? uri
            : new Uri($"https://{GoogleTokenHost}/token");

    private static bool IsRsaKey(string pem)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(pem);

            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
