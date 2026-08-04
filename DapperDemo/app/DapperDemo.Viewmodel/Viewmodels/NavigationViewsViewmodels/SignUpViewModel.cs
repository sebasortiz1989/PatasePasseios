using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Repository.Dapper;
using DapperDemo.Repository.Dapper.Aggregates;
using DapperDemo.Repository.Dapper.Dtos;
using DapperDemo.Repository.Dapper.Extensions;
using DapperDemo.Repository.Dapper.Services;
using DapperDemo.Viewmodel.Services;
using DapperDemo.Viewmodel.Viewmodels.Session;
using PropertyChanged;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.NavigationViewsViewmodels;

[AddINotifyPropertyChangedInterface]
public class SignUpViewModel : PresentationModelBase<Unit, Unit>
{
    private readonly RepositoryPetSitter repositoryPetSitter;
    private readonly AppSession session;
    private readonly NavigationController navigationController;
    private readonly Factory<PresenterBase<MainViewModel, Unit, Unit>> mainViewFactory;

    private readonly ImagePicker imagePicker;

    public SignUpViewModel(
        NavigationController navigationController,
        RepositoryPetSitter repositoryPetSitter,
        AppSession session,
        Factory<PresenterBase<MainViewModel, Unit, Unit>> mainViewFactory,
        ImagePicker imagePicker)
    {
        this.repositoryPetSitter = repositoryPetSitter;
        this.session = session;
        this.navigationController = navigationController;
        this.mainViewFactory = mainViewFactory;
        this.imagePicker = imagePicker;
        BackCommand = new SynchronizedCommand(() => navigationController.PopAsync(this), SynchronizationBehavior.Discard, true);
        BirthDate = DateTime.UtcNow - TimeSpan.FromDays(7000);
        RegisterCommand = new SynchronizedCommand(RegisterFunction, SynchronizationBehavior.Discard, true);
        ChoosePhotoCommand = new SynchronizedCommand(ChoosePhoto, SynchronizationBehavior.Discard, true);
        RemovePhotoCommand = new SynchronizedCommand(RemovePhoto, SynchronizationBehavior.Discard, true);
    }

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTime BirthDate { get; set; }

    public DateTime MinimumDate { get; } = new(1950, 1, 1);

    public string SignupError { get; set; } = string.Empty;

    public bool HasSignupError => !string.IsNullOrEmpty(SignupError);

    public ICommand BackCommand { get; }

    public ICommand RegisterCommand { get; }

    public ICommand ChoosePhotoCommand { get; }

    public ICommand RemovePhotoCommand { get; }

    /// <summary>Gets the picked photo's file name, written to the new account's Image column.</summary>
    public string PhotoFileName { get; private set; } = string.Empty;

    public string? PhotoPath => DogImageStore.ResolvePath(PhotoFileName);

    public bool HasPhoto => PhotoPath != null;

    public bool NoPhoto => !HasPhoto;

    protected override Task OnRunStarting(Unit input)
    {
        return Task.CompletedTask;
    }

    private async Task ChoosePhoto()
    {
        using var picked = await imagePicker.PickAsync().WithSync();
        if (picked == null)
        {
            return;
        }

        // Replacing a pick before registering: the earlier file is unreferenced, so it goes.
        DogImageStore.Delete(PhotoFileName);
        PhotoFileName = await DogImageStore.SaveAsync(picked.Content, picked.Extension).WithSync();
    }

    private Task RemovePhoto()
    {
        DogImageStore.Delete(PhotoFileName);
        PhotoFileName = string.Empty;
        return Task.CompletedTask;
    }

    private async Task RegisterFunction()
    {
        if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(Name))
        {
            SignupError = "Preencha todos os campos.";
            return;
        }

        var result = await repositoryPetSitter.Add(new PetSitter
        {
            Name = Name,
            Email = Email,
            Password = Password,
            BirthDate = BirthDate,
            PasswordHash = string.Empty,
            Image = string.IsNullOrEmpty(PhotoFileName) ? null : PhotoFileName,
        }).WithSync();

        if (result != Response.Successful)
        {
            // The photo is left on disk: the form stays open with it still shown, so the user can
            // fix the e-mail and register without picking it again. Abandoning the screen orphans
            // one file, which is the same trade the new-dog form makes.
            SignupError = result.GetDescription();
            return;
        }

        // Read the row back for its generated id: everything the new account creates is scoped to it.
        var petSitter = await repositoryPetSitter.GetByEmailAsync(Email).WithSync();
        if (petSitter == null)
        {
            SignupError = Response.Failed.GetDescription();
            return;
        }

        SignupError = string.Empty;
        Password = string.Empty;
        session.SignIn(petSitter);
        await navigationController.PushAsync(mainViewFactory.Create()).WithSync();
    }
}