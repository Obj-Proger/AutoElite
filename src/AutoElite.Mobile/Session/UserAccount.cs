using System.Globalization;
using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Authentication;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Session;

/// <summary>
/// The account of the signed-in user: their name, phone and the language of their notifications, their photo, a new
/// email address, which the API confirms by a link sent to it, and a new password, after which only this device stays
/// signed in. These need the network; the API checks every value again.
/// </summary>
/// <param name="createApi">Creates the client of the API.</param>
/// <param name="sessions">The session of the signed-in user.</param>
public sealed class UserAccount(Func<AutoEliteApiClient> createApi, AuthSessionManager sessions)
{
    /// <summary>Changes the name, the phone and the language of the notifications and emails.</summary>
    /// <param name="lastName">The last name.</param>
    /// <param name="firstName">The first name.</param>
    /// <param name="middleName">The middle name; empty removes it.</param>
    /// <param name="phone">The phone in E.164 format; empty removes it.</param>
    /// <param name="language">The language of the notifications and emails.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the API has saved them.</returns>
    public Task UpdateProfileAsync(
        string lastName, string firstName, string middleName, string phone, Language language, CancellationToken cancellationToken) =>
        createApi().Api.V1.Me.PutAsync(
            new UpdateMyProfileCommand
            {
                LastName = lastName.Trim(),
                FirstName = firstName.Trim(),
                MiddleName = middleName.Trim(),
                Phone = phone.Trim(),
                PreferredLanguage = language
            },
            cancellationToken: cancellationToken);

    /// <summary>Downloads a photo.</summary>
    /// <param name="documentId">The document of the photo.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The bytes of the image.</returns>
    public async Task<byte[]> ReadPhotoAsync(Guid documentId, CancellationToken cancellationToken)
    {
        await using var content = await createApi().Api.V1.Documents[documentId].GetAsync(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The API answered without the photo.");
        using var bytes = new MemoryStream();
        await content.CopyToAsync(bytes, cancellationToken);

        return bytes.ToArray();
    }

    /// <summary>
    /// Makes an image the photo of the user: it is attached to them as a document, which the API checks to be a JPEG,
    /// PNG or WebP image, and becomes their photo.
    /// </summary>
    /// <param name="userId">The signed-in user.</param>
    /// <param name="image">The image the user chose.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the photo is set.</returns>
    public async Task SetPhotoAsync(Guid userId, FileResult image, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(image);
        await using var content = await image.OpenReadAsync();
        var form = new MultipartBody();
        form.AddOrReplacePart("file", string.IsNullOrEmpty(image.ContentType) ? "image/jpeg" : image.ContentType, content, image.FileName);
        form.AddOrReplacePart("ownerKind", "text/plain", nameof(EntityKind.User));
        form.AddOrReplacePart("ownerId", "text/plain", userId.ToString("D", CultureInfo.InvariantCulture));
        form.AddOrReplacePart("description", "text/plain", "Profile photo");
        var document = await createApi().AttachDocumentAsync(form, cancellationToken);
        if (document.Id is not { } documentId)
            throw new InvalidOperationException("The API answered without the document of the photo.");

        await createApi().Api.V1.Me.Photo.PutAsync(new SetMyPhotoCommand { DocumentId = documentId }, cancellationToken: cancellationToken);
    }

    /// <summary>Removes the photo; the document stays among those of the user.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the photo is removed.</returns>
    public Task RemovePhotoAsync(CancellationToken cancellationToken) =>
        createApi().Api.V1.Me.Photo.DeleteAsync(cancellationToken: cancellationToken);

    /// <summary>Asks for a new email address, which the user confirms by the link the API sends to it.</summary>
    /// <param name="newEmail">The new address.</param>
    /// <param name="currentPassword">The current password, so that a device left signed in cannot take the account.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the link is sent.</returns>
    public Task RequestEmailChangeAsync(string newEmail, string currentPassword, CancellationToken cancellationToken) =>
        createApi().Api.V1.Me.Email.PostAsync(
            new RequestEmailChangeCommand { NewEmail = newEmail.Trim(), CurrentPassword = currentPassword },
            cancellationToken: cancellationToken);

    /// <summary>Changes the password; the other devices of the user are signed out, this one stays signed in.</summary>
    /// <param name="currentPassword">The current password.</param>
    /// <param name="newPassword">The new password.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the password is changed.</returns>
    public Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken) =>
        sessions.ChangePasswordAsync(currentPassword, newPassword, cancellationToken);
}
