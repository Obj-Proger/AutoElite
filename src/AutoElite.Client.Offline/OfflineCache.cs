using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Serialization.Json;

namespace AutoElite.Client.Offline;

/// <summary>
/// Keeps what the user last read from the API, so that the application shows it without a network: each read asks the
/// API first and keeps its answer under a key; when the API cannot be reached, the kept answer is given instead. The
/// answers are kept as the JSON of the models of the generated client.
/// </summary>
public sealed class OfflineCache
{
    private readonly OfflineDatabase _database;
    private readonly TimeProvider _timeProvider;

    // The JSON of the models goes through the serializers of Kiota, which the generated client registers when it is
    // created; they are registered here too, so that the cache can be read before any client is created.
    static OfflineCache()
    {
        ApiClientBuilder.RegisterDefaultSerializer<JsonSerializationWriterFactory>();
        ApiClientBuilder.RegisterDefaultDeserializer<JsonParseNodeFactory>();
    }

    /// <summary>Initializes a new instance of the <see cref="OfflineCache"/> class.</summary>
    /// <param name="database">The offline database.</param>
    /// <param name="timeProvider">The clock.</param>
    public OfflineCache(OfflineDatabase database, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _database = database;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Reads an object from the API and keeps it under the key; when the API cannot be reached, gives the one kept.
    /// </summary>
    /// <typeparam name="T">The model of the object.</typeparam>
    /// <param name="key">The key of the read, such as the path of the endpoint with its query.</param>
    /// <param name="fetch">Reads the object from the API.</param>
    /// <param name="factory">Creates the model, <c>CreateFromDiscriminatorValue</c> of the model.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The object, from the API or from the device.</returns>
    /// <exception cref="HttpRequestException">The API could not be reached and nothing is kept under the key.</exception>
    /// <exception cref="ApiException">The API refused the read.</exception>
    public async Task<Cached<T>> GetAsync<T>(
        string key, Func<CancellationToken, Task<T?>> fetch, ParsableFactory<T> factory, CancellationToken cancellationToken)
        where T : IParsable
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(fetch);
        ArgumentNullException.ThrowIfNull(factory);

        T? value;
        try
        {
            value = await fetch(cancellationToken);
        }
        catch (Exception exception) when (ApiFailures.IsTransient(exception, cancellationToken))
        {
            if (await ReadAsync(key, cancellationToken) is not { } kept)
                throw;

            var keptValue = await KiotaJsonSerializer.DeserializeAsync(kept.Content, factory, cancellationToken)
                ?? throw new InvalidOperationException($"The answer kept under '{key}' is empty.");

            return new Cached<T>(keptValue, kept.SavedAt, IsFromCache: true);
        }

        if (value is null)
            throw new InvalidOperationException($"The API answered the read '{key}' without content.");

        var content = await KiotaJsonSerializer.SerializeAsStringAsync(
            value, serializeOnlyChangedValues: false, cancellationToken: cancellationToken);
        var savedAt = await SaveAsync(key, content, cancellationToken);

        return new Cached<T>(value, savedAt, IsFromCache: false);
    }

    /// <summary>
    /// Reads a list from the API and keeps it under the key; when the API cannot be reached, gives the one kept.
    /// </summary>
    /// <typeparam name="T">The model of the items.</typeparam>
    /// <param name="key">The key of the read, such as the path of the endpoint with its query.</param>
    /// <param name="fetch">Reads the list from the API.</param>
    /// <param name="factory">Creates the model of an item, <c>CreateFromDiscriminatorValue</c> of the model.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The items, from the API or from the device.</returns>
    /// <exception cref="HttpRequestException">The API could not be reached and nothing is kept under the key.</exception>
    /// <exception cref="ApiException">The API refused the read.</exception>
    public async Task<Cached<IReadOnlyList<T>>> GetListAsync<T>(
        string key, Func<CancellationToken, Task<List<T>?>> fetch, ParsableFactory<T> factory, CancellationToken cancellationToken)
        where T : IParsable
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(fetch);
        ArgumentNullException.ThrowIfNull(factory);

        List<T>? items;
        try
        {
            items = await fetch(cancellationToken);
        }
        catch (Exception exception) when (ApiFailures.IsTransient(exception, cancellationToken))
        {
            if (await ReadAsync(key, cancellationToken) is not { } kept)
                throw;

            var keptItems = await KiotaJsonSerializer.DeserializeCollectionAsync(kept.Content, factory, cancellationToken);

            return new Cached<IReadOnlyList<T>>([.. keptItems], kept.SavedAt, IsFromCache: true);
        }

        IReadOnlyList<T> answer = items ?? [];
        var content = await KiotaJsonSerializer.SerializeAsStringAsync<T>(
            answer, serializeOnlyChangedValues: false, cancellationToken: cancellationToken);
        var savedAt = await SaveAsync(key, content, cancellationToken);

        return new Cached<IReadOnlyList<T>>(answer, savedAt, IsFromCache: false);
    }

    private Task<Kept?> ReadAsync(string key, CancellationToken cancellationToken) =>
        _database.RunAsync<Kept?>(connection =>
        {
            using var command = OfflineDatabase.Command(
                connection, "SELECT content, saved_at FROM snapshots WHERE key = $key", ("$key", key));
            using var reader = command.ExecuteReader();

            return reader.Read()
                ? new Kept(reader.GetString(0), DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(1)))
                : null;
        }, cancellationToken);

    private Task<DateTimeOffset> SaveAsync(string key, string content, CancellationToken cancellationToken)
    {
        // To the millisecond, as the database keeps it, so that a kept answer has the time it was given with.
        var savedAt = DateTimeOffset.FromUnixTimeMilliseconds(_timeProvider.GetUtcNow().ToUnixTimeMilliseconds());

        return _database.RunAsync(connection =>
        {
            OfflineDatabase.Execute(
                connection,
                """
                INSERT INTO snapshots (key, content, saved_at) VALUES ($key, $content, $savedAt)
                ON CONFLICT (key) DO UPDATE SET content = excluded.content, saved_at = excluded.saved_at
                """,
                ("$key", key),
                ("$content", content),
                ("$savedAt", savedAt.ToUnixTimeMilliseconds()));

            return savedAt;
        }, cancellationToken);
    }

    // An answer kept on the device.
    private sealed record Kept(string Content, DateTimeOffset SavedAt);
}
