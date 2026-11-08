using System.Data.Common;
using AutoElite.Infrastructure.Storage;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace AutoElite.Infrastructure.Tests.Storage;

/// <summary>
/// The S3-compatible storage of the tests. When the <see cref="ServerVariable"/> environment variable holds the
/// settings of a storage already running elsewhere, as <c>ServiceUrl=...;AccessKey=...;SecretKey=...</c> with an
/// optional <c>Region=...</c>, the tests use it: for a server where containers with published ports are not wanted.
/// Otherwise they start SeaweedFS in a throwaway container, which needs a local Docker engine, as in CI.
/// </summary>
/// <remarks>
/// Without either the storage is missing and the tests that need it are skipped, with <see cref="MissingReason"/>,
/// since it serves only one of the two file storages. In CI, which has Docker and must check that storage, a missing
/// Docker fails the tests instead.
/// </remarks>
internal static class TestS3
{
    public const string ServerVariable = "AUTOELITE_TEST_S3";

    /// <summary>Why the tests of the S3 storage are skipped when there is no storage.</summary>
    public const string MissingReason =
        "No S3-compatible storage: start Docker, or set " + ServerVariable + " to the settings of a running storage "
        + "(see the README). CI runs these tests.";

    private const int S3Port = 8333;

    // The account of the throwaway container; SeaweedFS refuses signed requests until it has one.
    private const string ContainerAccessKey = "autoelite-test";

    private const string ContainerSecretKey = "autoelite-test-secret";

    private static readonly Lazy<Task<S3StorageOptions?>> _server = new(StartAsync);

    // Kept for the whole run; the Testcontainers resource reaper removes it when the test process exits.
    private static IContainer? _container;

    /// <summary>Gets a value indicating whether the storage is a container started for the run.</summary>
    public static bool IsContainer => _container is not null;

    /// <summary>Gets the settings of the storage, without a bucket.</summary>
    /// <returns>The settings, or <see langword="null"/> when there is no storage outside CI.</returns>
    public static Task<S3StorageOptions?> GetServerAsync() => _server.Value;

    private static async Task<S3StorageOptions?> StartAsync()
    {
        var server = Environment.GetEnvironmentVariable(ServerVariable);
        if (!string.IsNullOrWhiteSpace(server))
            return Parse(server);

        IContainer container;
        try
        {
            // Without a configuration file, SeaweedFS makes an administrator of the keys in these variables.
            container = new ContainerBuilder("chrislusf/seaweedfs:4.48")
                .WithCommand("server", "-s3", "-dir=/data")
                .WithEnvironment("AWS_ACCESS_KEY_ID", ContainerAccessKey)
                .WithEnvironment("AWS_SECRET_ACCESS_KEY", ContainerSecretKey)
                .WithPortBinding(S3Port, assignRandomHostPort: true)
                .WithWaitStrategy(Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(request => request.ForPort(S3Port).ForStatusCodeMatching(_ => true)))
                .Build();
        }
        catch (DockerUnavailableException) when (!IsCi)
        {
            return null;
        }

        _container = container;
        await _container.StartAsync();

        return new S3StorageOptions
        {
            ServiceUrl = $"http://{_container.Hostname}:{_container.GetMappedPublicPort(S3Port)}",
            AccessKey = ContainerAccessKey,
            SecretKey = ContainerSecretKey
        };
    }

    private static bool IsCi => string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);

    private static S3StorageOptions Parse(string server)
    {
        var settings = new DbConnectionStringBuilder { ConnectionString = server };
        string Required(string key) =>
            settings.TryGetValue(key, out var value) && value is string text && !string.IsNullOrWhiteSpace(text)
                ? text
                : throw new InvalidOperationException($"{ServerVariable} must hold {key}=...");

        var options = new S3StorageOptions
        {
            ServiceUrl = Required("ServiceUrl"),
            AccessKey = Required("AccessKey"),
            SecretKey = Required("SecretKey")
        };
        if (settings.TryGetValue("Region", out var region) && region is string regionName)
            options.Region = regionName;

        return options;
    }
}
