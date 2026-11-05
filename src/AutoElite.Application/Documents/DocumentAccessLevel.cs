namespace AutoElite.Application.Documents;

/// <summary>What the caller may do with the documents of an owner record; each level includes the ones before it.</summary>
internal enum DocumentAccessLevel
{
    /// <summary>The caller may not see the owner's documents; the owner is reported as not found.</summary>
    None = 0,

    /// <summary>The caller may list and download the documents.</summary>
    Read = 1,

    /// <summary>The caller may also attach documents and delete or describe the ones they attached.</summary>
    Contribute = 2,

    /// <summary>The caller may also delete or describe documents attached by others.</summary>
    Manage = 3
}
