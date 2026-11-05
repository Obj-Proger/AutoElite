using AutoElite.Application.Documents;
using AutoElite.Domain.Entities;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeDocumentRepository : InMemoryRepository<Document>, IDocumentRepository;
