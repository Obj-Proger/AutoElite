namespace AutoElite.Application.Documents;

/// <summary>Loads, adds and removes documents for commands.</summary>
public interface IDocumentRepository : IRepository<Document>
{
    /// <summary>Removes a document; it is deleted on the next save.</summary>
    /// <param name="document">The document to remove.</param>
    void Remove(Document document);
}
