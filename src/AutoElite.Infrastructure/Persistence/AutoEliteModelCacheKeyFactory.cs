using Microsoft.EntityFrameworkCore.Infrastructure;

namespace AutoElite.Infrastructure.Persistence;

/// <summary>
/// Keys the cached model of <see cref="AutoEliteDbContext"/> by the keys of its chat cipher as well, since the model
/// holds the cipher in a value converter: a context with other keys must not reuse a model built with the first ones.
/// </summary>
internal sealed class AutoEliteModelCacheKeyFactory : IModelCacheKeyFactory
{
    /// <inheritdoc />
    public object Create(DbContext context, bool designTime) =>
        context is AutoEliteDbContext autoElite
            ? (context.GetType(), autoElite.ChatTextCipher.Fingerprint, designTime)
            : (context.GetType(), designTime);
}
