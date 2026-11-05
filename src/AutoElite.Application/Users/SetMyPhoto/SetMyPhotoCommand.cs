namespace AutoElite.Application.Users.SetMyPhoto;

/// <summary>
/// Makes one of the signed-in user's documents their profile photo (FR-1.7): attach the image to the user first with
/// <see cref="Documents.AttachDocument.AttachDocumentCommand"/>. Everyone signed in can then download the photo, while
/// the user's other documents stay private.
/// </summary>
/// <param name="DocumentId">The document with the photo, a JPEG, PNG or WebP image.</param>
public sealed record SetMyPhotoCommand(Guid DocumentId) : ICommand;
