using System.Collections.ObjectModel;
using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// A question of the school that a theory instructor writes or corrects: its text, the topic of a course, the licence
/// categories, from two to six options with one correct, an explanation and an illustration. A question is taken out
/// of new tests, or put back, rather than deleted. Saving needs the network; the API checks every value again.
/// </summary>
public sealed class QuestionViewModel : ViewModel, IQueryAttributable
{
    /// <summary>The navigation parameter that carries the identifier of the question to correct.</summary>
    public const string QuestionParameter = "question";

    // The options a question has at least and at most, as the API allows.
    private const int MinOptions = 2;
    private const int MaxOptions = 6;

    private readonly QuestionBank _bank;
    private IReadOnlyList<CourseListItem> _courses = [];
    private IReadOnlyList<Guid> _topicIds = [];
    private Guid? _questionId;
    private Guid? _topicToSelect;
    private Guid? _imageId;
    private FileResult? _newImage;
    private string? _externalCode;
    private bool _isLoaded;
    private bool _isLoadingTopics;

    /// <summary>Initializes a new instance of the <see cref="QuestionViewModel"/> class.</summary>
    /// <param name="bank">The questions of the school.</param>
    public QuestionViewModel(QuestionBank bank)
    {
        _bank = bank;
        Categories = [.. Enum.GetValues<LicenseCategory>().Select(category => new CategoryEntry(category))];
        SaveCommand = new Command(async () => await SaveAsync(), () => !IsBusy);
        AddOptionCommand = new Command(AddOption, () => Options.Count < MaxOptions);
        ChangeImageCommand = new Command(async () => await ChangeImageAsync(), () => !IsBusy);
        RemoveImageCommand = new Command(RemoveImage, () => !IsBusy);
        ToggleActiveCommand = new Command(async () => await ToggleActiveAsync(), () => !IsBusy);
        for (var option = 0; option < 3; option++)
            AddOption();
    }

    /// <summary>Gets the title of the page: a new question, or correcting one.</summary>
    public string Title
    {
        get;
        private set => SetProperty(ref field, value);
    } = AppTexts.Get("Question.NewTitle");

    /// <summary>Gets a value indicating whether an existing question is corrected.</summary>
    public bool IsExisting
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets or sets the text of the question.</summary>
    public string Text
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets or sets the explanation shown after the test.</summary>
    public string Explanation
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the active courses.</summary>
    public IReadOnlyList<string> Courses
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets or sets the course, by its position; choosing one reads its topics.</summary>
    public int CourseIndex
    {
        get;
        set
        {
            if (SetProperty(ref field, value) && !_isLoadingTopics)
                _ = LoadTopicsAsync(selectCategory: true);
        }
    } = -1;

    /// <summary>Gets the topics of the course, in their order.</summary>
    public IReadOnlyList<string> Topics
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets or sets the topic, by its position.</summary>
    public int TopicIndex
    {
        get;
        set => SetProperty(ref field, value);
    } = -1;

    /// <summary>Gets the licence categories, with those the question applies to selected.</summary>
    public IReadOnlyList<CategoryEntry> Categories { get; }

    /// <summary>Gets the answer options, in their order.</summary>
    public ObservableCollection<OptionEntry> Options { get; } = [];

    /// <summary>Gets the illustration, or <see langword="null"/> without one.</summary>
    public ImageSource? Image
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasImage));
        }
    }

    /// <summary>Gets a value indicating whether the question has an illustration.</summary>
    public bool HasImage => Image is not null;

    /// <summary>Gets a value indicating whether new tests use the question.</summary>
    public bool IsActive
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(ActiveText));
        }
    } = true;

    /// <summary>Gets what the button of taking the question out of new tests, or putting it back, says.</summary>
    public string ActiveText => IsActive ? AppTexts.Get("Question.Deactivate") : AppTexts.Get("Question.Reactivate");

    /// <summary>Gets why the question could not be saved, or that it is out of new tests, or <see langword="null"/>.</summary>
    public string? Message
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasMessage));
        }
    }

    /// <summary>Gets a value indicating whether there is a message.</summary>
    public bool HasMessage => Message is not null;

    /// <summary>Gets a value indicating whether the question is being read or saved.</summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (!SetProperty(ref field, value))
                return;

            SaveCommand.ChangeCanExecute();
            ChangeImageCommand.ChangeCanExecute();
            RemoveImageCommand.ChangeCanExecute();
            ToggleActiveCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets the command that saves the question.</summary>
    public Command SaveCommand { get; }

    /// <summary>Gets the command that adds an option, up to six.</summary>
    public Command AddOptionCommand { get; }

    /// <summary>Gets the command that chooses an illustration.</summary>
    public Command ChangeImageCommand { get; }

    /// <summary>Gets the command that removes the illustration when the question is saved.</summary>
    public Command RemoveImageCommand { get; }

    /// <summary>Gets the command that takes the question out of new tests, or puts it back.</summary>
    public Command ToggleActiveCommand { get; }

    /// <inheritdoc />
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue(QuestionParameter, out var value) && value is Guid questionId)
        {
            _questionId = questionId;
            IsExisting = true;
            Title = AppTexts.Get("Question.EditTitle");
        }
    }

    /// <summary>Reads the courses and, for a question to correct, the question; once, when the page first appears.</summary>
    /// <returns>A task that completes when the page shows them.</returns>
    public async Task LoadAsync()
    {
        if (_isLoaded)
            return;

        _isLoaded = true;
        IsBusy = true;
        try
        {
            var courses = await _bank.ReadCoursesAsync(CancellationToken.None);
            _courses = [.. courses.Value.Where(course => course.Id is not null)];
            Courses = [.. _courses.Select(course => course.Name ?? string.Empty)];
            if (_questionId is { } questionId)
                await ShowAsync(await _bank.ReadAsync(questionId, CancellationToken.None));
            else if (_courses.Count == 1)
                CourseIndex = 0;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException
            or InvalidOperationException)
        {
            Message = ProblemTexts.Of(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ShowAsync(QuestionDetails question)
    {
        Text = question.Text ?? string.Empty;
        Explanation = question.Explanation ?? string.Empty;
        _externalCode = question.ExternalCode;
        IsActive = question.IsActive != false;
        Message = IsActive ? null : AppTexts.Get("Question.InactiveNote");
        var selected = (question.Categories ?? []).OfType<LicenseCategory>().ToHashSet();
        foreach (var category in Categories)
            category.IsSelected = selected.Contains(category.Category);

        Options.Clear();
        foreach (var option in (question.Options ?? []).OrderBy(option => option.Position))
            AddOption(option.Text ?? string.Empty, option.IsCorrect == true);

        _topicToSelect = question.TopicId;
        _isLoadingTopics = true;
        CourseIndex = _courses.ToList().FindIndex(course => course.Id == question.CourseId);
        _isLoadingTopics = false;
        await LoadTopicsAsync(selectCategory: false);

        _imageId = question.ImageDocumentId;
        if (_imageId is { } imageId)
        {
            var bytes = await _bank.ReadImageAsync(imageId, CancellationToken.None);
            Image = ImageSource.FromStream(() => new MemoryStream(bytes));
        }
    }

    // Reads the topics of the course chosen; a new choice of the course proposes its category.
    private async Task LoadTopicsAsync(bool selectCategory)
    {
        _topicIds = [];
        Topics = [];
        TopicIndex = -1;
        if (CourseIndex < 0 || CourseIndex >= _courses.Count)
            return;

        var chosen = _courses[CourseIndex];
        if (selectCategory && chosen.Category is { } courseCategory && !Categories.Any(category => category.IsSelected))
            Categories.First(category => category.Category == courseCategory).IsSelected = true;

        try
        {
            var course = await _bank.ReadCourseAsync(chosen.Id!.Value, CancellationToken.None);
            List<CourseTopicItem> topics =
                [.. (course.Value.Topics ?? []).Where(topic => topic.Id is not null).OrderBy(topic => topic.Position)];
            _topicIds = [.. topics.Select(topic => topic.Id!.Value)];
            Topics = [.. topics.Select(topic => string.Create(CultureInfo.CurrentCulture, $"{topic.Position}. {topic.Title}"))];
            TopicIndex = _topicToSelect is { } topicId ? _topicIds.ToList().IndexOf(topicId) : -1;
            _topicToSelect = null;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Message = ProblemTexts.Of(exception);
        }
    }

    private void AddOption() => AddOption(string.Empty, isCorrect: false);

    private void AddOption(string text, bool isCorrect)
    {
        if (Options.Count >= MaxOptions)
            return;

        var option = new OptionEntry { Text = text, IsCorrect = isCorrect };
        option.CorrectCommand = new Command(() =>
        {
            foreach (var other in Options)
                other.IsCorrect = ReferenceEquals(other, option);
        });
        option.RemoveCommand = new Command(() => RemoveOption(option));
        Options.Add(option);
        Renumber();
    }

    private void RemoveOption(OptionEntry option)
    {
        if (Options.Count <= MinOptions)
            return;

        Options.Remove(option);
        Renumber();
    }

    private void Renumber()
    {
        for (var index = 0; index < Options.Count; index++)
            Options[index].Number = (index + 1).ToString(CultureInfo.CurrentCulture);

        AddOptionCommand.ChangeCanExecute();
    }

    private async Task ChangeImageAsync()
    {
        try
        {
            var image = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = AppTexts.Get("Question.ChooseImage"),
                FileTypes = FilePickerFileType.Images
            });
            if (image is null)
                return;

            await using var content = await image.OpenReadAsync();
            using var bytes = new MemoryStream();
            await content.CopyToAsync(bytes);
            var data = bytes.ToArray();
            _newImage = image;
            Image = ImageSource.FromStream(() => new MemoryStream(data));
        }
        catch (PermissionException)
        {
            Message = AppTexts.Get("Question.NoImageAccess");
        }
    }

    private void RemoveImage()
    {
        _newImage = null;
        Image = null;
    }

    private QuestionDraft? Draft()
    {
        if (TopicIndex < 0 || TopicIndex >= _topicIds.Count)
        {
            Message = AppTexts.Get("Question.ChooseTopic");

            return null;
        }

        List<LicenseCategory> categories = [.. Categories.Where(category => category.IsSelected).Select(category => category.Category)];
        if (categories.Count == 0)
        {
            Message = AppTexts.Get("Question.ChooseCategory");

            return null;
        }

        if (Options.Count(option => option.IsCorrect) != 1)
        {
            Message = AppTexts.Get("Question.ChooseCorrect");

            return null;
        }

        return new QuestionDraft(
            Text.Trim(),
            string.IsNullOrWhiteSpace(Explanation) ? null : Explanation.Trim(),
            _topicIds[TopicIndex],
            categories,
            _externalCode,
            [.. Options.Select(option => (option.Text.Trim(), option.IsCorrect))]);
    }

    private async Task SaveAsync()
    {
        if (Draft() is not { } draft)
            return;

        IsBusy = true;
        Message = null;
        try
        {
            Guid questionId;
            if (_questionId is { } existing)
            {
                questionId = existing;
                await _bank.UpdateAsync(questionId, draft, CancellationToken.None);
            }
            else
            {
                questionId = await _bank.CreateAsync(draft, CancellationToken.None);
                _questionId = questionId;
            }

            // The illustration follows the content: a new image replaces the old one, a removed one goes.
            if (_newImage is { } image)
            {
                await _bank.SetImageAsync(questionId, image, CancellationToken.None);
                _newImage = null;
            }
            else if (Image is null && _imageId is not null)
            {
                await _bank.RemoveImageAsync(questionId, CancellationToken.None);
                _imageId = null;
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException
            or InvalidOperationException or IOException)
        {
            Message = ProblemTexts.Of(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ToggleActiveAsync()
    {
        if (_questionId is not { } questionId)
            return;

        if (IsActive && !await Shell.Current.DisplayAlertAsync(
                AppTexts.Get("Question.DeactivateTitle"),
                AppTexts.Get("Question.DeactivateQuestion"),
                AppTexts.Get("Question.Deactivate"),
                AppTexts.Get("Common.Cancel")))
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _bank.SetActiveAsync(questionId, !IsActive, CancellationToken.None);
            IsActive = !IsActive;
            Message = IsActive ? null : AppTexts.Get("Question.InactiveNote");
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            Message = ProblemTexts.Of(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
