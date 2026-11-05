namespace AutoElite.Application.Questions.SetQuestionImage;

/// <summary>
/// Makes a document the illustration of a question: attach the image to the question first with
/// <see cref="Documents.AttachDocument.AttachDocumentCommand"/>. Attempts already drawn keep the illustration they
/// showed.
/// </summary>
/// <param name="QuestionId">The question.</param>
/// <param name="DocumentId">The document with the image, a JPEG, PNG or WebP image attached to the question.</param>
[RequireRoles(UserRole.Administrator, UserRole.TheoryInstructor)]
public sealed record SetQuestionImageCommand(Guid QuestionId, Guid DocumentId) : ICommand;
