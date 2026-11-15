using System.Globalization;
using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// The questions the school writes for the practice tests, which a theory instructor finds, writes and corrects: the
/// text, the topic of a course, the licence categories, from two to six options with one correct, an explanation and
/// an illustration. A question is taken out of new tests rather than deleted, since tests already taken keep it. These
/// need the network; the courses and their topics are read from the API or, without a network, from the device.
/// </summary>
/// <param name="cache">What the user last read.</param>
/// <param name="createApi">Creates the client of the API.</param>
public sealed class QuestionBank(OfflineCache cache, Func<AutoEliteApiClient> createApi)
{
    /// <summary>The questions read at a time.</summary>
    public const int PageSize = 20;

    /// <summary>Reads a page of the questions of the school, the latest added first.</summary>
    /// <param name="search">Text in the question, or empty for all.</param>
    /// <param name="page">The page, from 1.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The page.</returns>
    public async Task<PagedListOfQuestionListItem> ReadPageAsync(string search, int page, CancellationToken cancellationToken) =>
        await createApi().Api.V1.Questions.GetAsync(
            request =>
            {
                request.QueryParameters.Source = QuestionSource.School;
                request.QueryParameters.Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
                request.QueryParameters.Page = page;
                request.QueryParameters.PageSize = PageSize;
            },
            cancellationToken)
        ?? new PagedListOfQuestionListItem { Items = [] };

    /// <summary>Reads a question with its options and the correct answer.</summary>
    /// <param name="questionId">The question.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The question.</returns>
    public async Task<QuestionDetails> ReadAsync(Guid questionId, CancellationToken cancellationToken) =>
        await createApi().Api.V1.Questions[questionId].GetAsync(cancellationToken: cancellationToken)
        ?? throw new InvalidOperationException("The API answered without the question.");

    /// <summary>Reads the active courses, whose topics the questions belong to.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The courses.</returns>
    public Task<Cached<IReadOnlyList<CourseListItem>>> ReadCoursesAsync(CancellationToken cancellationToken) =>
        cache.GetListAsync(
            "courses/active",
            token => createApi().Api.V1.Courses.GetAsync(request => request.QueryParameters.IsActive = true, token),
            CourseListItem.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Reads a course with its topics.</summary>
    /// <param name="courseId">The course.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The course.</returns>
    public Task<Cached<CourseDetails>> ReadCourseAsync(Guid courseId, CancellationToken cancellationToken) =>
        cache.GetAsync(
            string.Create(CultureInfo.InvariantCulture, $"courses/{courseId:N}"),
            token => createApi().Api.V1.Courses[courseId].GetAsync(cancellationToken: token),
            CourseDetails.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Adds a question of the school.</summary>
    /// <param name="content">The content of the question.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The question.</returns>
    public async Task<Guid> CreateAsync(QuestionDraft content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        var created = await createApi().Api.V1.Questions.PostAsync(
            new CreateQuestionCommand
            {
                Text = content.Text,
                Explanation = content.Explanation,
                TopicId = content.TopicId,
                Categories = [.. content.Categories.Select(category => (LicenseCategory?)category)],
                Source = QuestionSource.School,
                Options = OptionsOf(content)
            },
            cancellationToken: cancellationToken);

        return created?.Id ?? throw new InvalidOperationException("The API answered without the question.");
    }

    /// <summary>Replaces the content of an active question; tests already taken keep it as it was asked.</summary>
    /// <param name="questionId">The question.</param>
    /// <param name="content">The new content.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the question is saved.</returns>
    public Task UpdateAsync(Guid questionId, QuestionDraft content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        return createApi().Api.V1.Questions[questionId].PutAsync(
            new UpdateQuestionCommand
            {
                QuestionId = questionId,
                Text = content.Text,
                Explanation = content.Explanation,
                TopicId = content.TopicId,
                Categories = [.. content.Categories.Select(category => (LicenseCategory?)category)],
                ExternalCode = content.ExternalCode,
                Options = OptionsOf(content)
            },
            cancellationToken: cancellationToken);
    }

    /// <summary>Takes a question out of new tests, or puts it back.</summary>
    /// <param name="questionId">The question.</param>
    /// <param name="isActive">Whether new tests use the question.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the question is changed.</returns>
    public Task SetActiveAsync(Guid questionId, bool isActive, CancellationToken cancellationToken) =>
        isActive
            ? createApi().Api.V1.Questions[questionId].Reactivate.PostAsync(cancellationToken: cancellationToken)
            : createApi().Api.V1.Questions[questionId].Deactivate.PostAsync(cancellationToken: cancellationToken);

    /// <summary>
    /// Makes an image the illustration of a question: it is attached to the question as a document, which the API checks
    /// to be a JPEG, PNG or WebP image.
    /// </summary>
    /// <param name="questionId">The question.</param>
    /// <param name="image">The image the user chose.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the illustration is set.</returns>
    public async Task SetImageAsync(Guid questionId, FileResult image, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(image);
        await using var content = await image.OpenReadAsync();
        var form = new MultipartBody();
        form.AddOrReplacePart("file", string.IsNullOrEmpty(image.ContentType) ? "image/jpeg" : image.ContentType, content, image.FileName);
        form.AddOrReplacePart("ownerKind", "text/plain", nameof(EntityKind.Question));
        form.AddOrReplacePart("ownerId", "text/plain", questionId.ToString("D", CultureInfo.InvariantCulture));
        var document = await createApi().AttachDocumentAsync(form, cancellationToken);
        await createApi().Api.V1.Questions[questionId].Image.PutAsync(
            new SetQuestionImageCommand { QuestionId = questionId, DocumentId = document.Id }, cancellationToken: cancellationToken);
    }

    /// <summary>Removes the illustration of a question.</summary>
    /// <param name="questionId">The question.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the illustration is removed.</returns>
    public Task RemoveImageAsync(Guid questionId, CancellationToken cancellationToken) =>
        createApi().Api.V1.Questions[questionId].Image.DeleteAsync(cancellationToken: cancellationToken);

    /// <summary>Downloads an illustration.</summary>
    /// <param name="documentId">The document of the image.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The bytes of the image.</returns>
    public async Task<byte[]> ReadImageAsync(Guid documentId, CancellationToken cancellationToken)
    {
        await using var content = await createApi().Api.V1.Documents[documentId].GetAsync(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The API answered without the image.");
        using var bytes = new MemoryStream();
        await content.CopyToAsync(bytes, cancellationToken);

        return bytes.ToArray();
    }

    private static List<AnswerOptionInput> OptionsOf(QuestionDraft content) =>
        [.. content.Options.Select(option => new AnswerOptionInput { Text = option.Text, IsCorrect = option.IsCorrect })];
}
