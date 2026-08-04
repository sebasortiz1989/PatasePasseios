using System.Windows.Input;
using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Repository.Dapper;
using DapperDemo.Repository.Dapper.Aggregates;
using DapperDemo.Repository.Dapper.Extensions;
using DapperDemo.Repository.Dapper.Services;
using DapperDemo.Viewmodel.Services;
using DapperDemo.Viewmodel.Viewmodels.Session;
using PropertyChanged;

namespace DapperDemo.Viewmodel.Viewmodels.NavigationViewsViewmodels;

[AddINotifyPropertyChangedInterface]
public class LoginViewModel : PresentationModelBase<Unit, Unit>
{
    private readonly NavigationController navigationController;
    private readonly RepositoryPetSitter repositoryPetSitter;
    private readonly BackupFileDialog backupFileDialog;
    private readonly BackupArchive backupArchive;
    private readonly SynchronizationContext synchronizationContext;
    private readonly AppSession session;
    private readonly Factory<PresenterBase<SignUpViewModel, Unit, Unit>> signUpViewFactory;
    private readonly Factory<PresenterBase<MainViewModel, Unit, Unit>> mainViewFactory;

    public LoginViewModel(
        NavigationController navigationController,
        RepositoryPetSitter repositoryPetSitter,
        BackupFileDialog backupFileDialog,
        BackupArchive backupArchive,
        SynchronizationContext synchronizationContext,
        AppSession session,
        Factory<PresenterBase<SignUpViewModel, Unit, Unit>> signUpViewFactory,
        Factory<PresenterBase<MainViewModel, Unit, Unit>> mainViewFactory)
    {
        this.navigationController = navigationController;
        this.repositoryPetSitter = repositoryPetSitter;
        this.backupFileDialog = backupFileDialog;
        this.backupArchive = backupArchive;
        this.synchronizationContext = synchronizationContext;
        this.session = session;
        this.signUpViewFactory = signUpViewFactory;
        this.mainViewFactory = mainViewFactory;
        LoginCommand = new SynchronizedCommand(LoginFunction, SynchronizationBehavior.Discard, true);
        SignUpCommand = new SynchronizedCommand(SignUpCommandFunction, SynchronizationBehavior.Discard, true);
        ImportBackupCommand = new SynchronizedCommand(ImportBackup, SynchronizationBehavior.Discard, true);
        DismissInvalidBackupCommand = new SynchronizedCommand(() => ShowInvalidBackupAlert = false, SynchronizationBehavior.Discard, true);
    }

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string LoginError { get; set; } = string.Empty;

    public bool HasLoginError => !string.IsNullOrEmpty(LoginError);

    public ICommand LoginCommand { get; }

    public ICommand SignUpCommand { get; }

    /// <summary>
    /// Restores a backup before anyone has signed in.
    /// </summary>
    /// <remarks>
    /// The same restore the profile screen offers, reachable from here because the case that
    /// needs it most is a fresh install or a new phone — where there is no account to sign into
    /// yet, so the profile screen cannot be reached at all.
    /// </remarks>
    public ICommand ImportBackupCommand { get; }

    public ICommand DismissInvalidBackupCommand { get; }

    /// <summary>Gets a value indicating whether the "not a valid backup" popup is showing.</summary>
    public bool ShowInvalidBackupAlert { get; private set; }

    /// <summary>Gets the outcome of the last import, shown under the buttons.</summary>
    public string BackupMsg { get; private set; } = string.Empty;

    public bool HasBackupMsg => !string.IsNullOrEmpty(BackupMsg);

    protected override Task OnRunStarting(Unit input)
    {
        // GetAll fires its callback from a Task.Run, so the assignment to Email — which raises
        // PropertyChanged into a binding — has to be marshalled back onto the UI thread.
        repositoryPetSitter.GetAll(perSitters => synchronizationContext.Run(() =>
        {
            if (perSitters.Length != 0)
            {
                Email = perSitters[^1].Email;
            }
        }));

        return Task.CompletedTask;
    }

    private async Task LoginFunction()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            LoginError = "Preencha email e senha.";
            return;
        }

        var response = repositoryPetSitter.VerifyLogin(Email, Password);
        if (response != Response.Successful)
        {
            LoginError = response.GetDescription();
            return;
        }

        // Everything after login is scoped to this account, so the row has to be loaded before
        // navigating — VerifyLogin only reports success, it doesn't say who signed in.
        var petSitter = await repositoryPetSitter.GetByEmailAsync(Email).WithSync();
        if (petSitter == null)
        {
            LoginError = Response.EmailDoesNotExists.GetDescription();
            return;
        }

        LoginError = string.Empty;
        Password = string.Empty;
        session.SignIn(petSitter);
        await navigationController.PushAsync(mainViewFactory.Create()).WithSync();
    }

    private async Task ImportBackup()
    {
        BackupMsg = string.Empty;

        var source = await backupFileDialog.OpenAsync().WithSync();
        if (source == null)
        {
            return;
        }

        Response result;
        await using (source.ConfigureAwait(true))
        {
            result = await backupArchive.RestoreFromAsync(source).WithSync();
        }

        if (result != Response.Successful)
        {
            // The popup carries the whole message; a second copy under the button would read as a
            // failure that had not gone away.
            ShowInvalidBackupAlert = true;
            return;
        }

        // The restored file brings its own accounts, so the e-mail on screen may no longer exist.
        // Clearing both fields is less confusing than leaving a stale one behind.
        Email = string.Empty;
        Password = string.Empty;
        LoginError = string.Empty;
        BackupMsg = "Backup importado. Entre com a conta restaurada.";
    }

    private async Task SignUpCommandFunction() => await navigationController.PushAsync(signUpViewFactory.Create()).WithSync();
}