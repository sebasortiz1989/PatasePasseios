using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Repository.Dapper;
using DapperDemo.Repository.Dapper.Aggregates;
using DapperDemo.Repository.Dapper.Dtos;
using DapperDemo.Viewmodel.Reports;
using DapperDemo.Viewmodel.Viewmodels.Session;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels;

[AddINotifyPropertyChangedInterface]
public class TutorDetailViewModel : PresentationModelBase<Unit, Unit>
{
    private static readonly CultureInfo Brazil = new("pt-BR");

    private readonly RepositoryTutors repositoryTutors;
    private readonly RepositoryDogs repositoryDogs;
    private readonly RepositoryServices repositoryServices;
    private readonly RepositoryPetSitter repositoryPetSitter;
    private readonly ReportExporter reportExporter;
    private readonly AppSession session;
    private readonly CurrentView currentView;

    /// <summary>Every service for this tutor's dogs, kept so the PDF does not re-read them.</summary>
    private ServiceItem[] tutorServices = [];

    public TutorDetailViewModel(
        CurrentView currentView,
        RepositoryTutors repositoryTutors,
        RepositoryDogs repositoryDogs,
        RepositoryServices repositoryServices,
        RepositoryPetSitter repositoryPetSitter,
        ReportExporter reportExporter,
        AppSession session)
    {
        this.repositoryTutors = repositoryTutors;
        this.repositoryDogs = repositoryDogs;
        this.repositoryServices = repositoryServices;
        this.repositoryPetSitter = repositoryPetSitter;
        this.reportExporter = reportExporter;
        this.session = session;
        this.currentView = currentView;
        BackCommand = new SynchronizedCommand(currentView.GoBack, SynchronizationBehavior.Discard, true);
        AskDeleteCommand = new SynchronizedCommand(() => ConfirmingDelete = true, SynchronizationBehavior.Discard, true);
        CancelDeleteCommand = new SynchronizedCommand(() => ConfirmingDelete = false, SynchronizationBehavior.Discard, true);
        ConfirmDeleteCommand = new SynchronizedCommand(Delete, SynchronizationBehavior.Discard, true);
        EditCommand = new SynchronizedCommand(StartEdit, SynchronizationBehavior.Discard, true);
        CancelEditCommand = new SynchronizedCommand(CancelEdit, SynchronizationBehavior.Discard, true);
        SaveEditCommand = new SynchronizedCommand(SaveEdit, SynchronizationBehavior.Discard, true);
        ExportCommand = new SynchronizedCommand(Export, SynchronizationBehavior.Discard, true);
    }

    public ICommand BackCommand { get; }

    public ICommand AskDeleteCommand { get; }

    public ICommand CancelDeleteCommand { get; }

    public ICommand ConfirmDeleteCommand { get; }

    public ICommand EditCommand { get; }

    public ICommand CancelEditCommand { get; }

    public ICommand SaveEditCommand { get; }

    public ICommand ExportCommand { get; }

    /// <summary>Gets a value indicating whether deleting takes two taps: the button swaps for a confirm/cancel pair.</summary>
    public bool ConfirmingDelete { get; private set; }

    public bool NotConfirmingDelete => !ConfirmingDelete;

    /// <summary>
    /// Gets a value indicating whether the screen is in edit mode — see
    /// <see cref="DogDetailViewModel.IsEditing"/> for why the editor replaces the fields in place
    /// rather than opening a separate form.
    /// </summary>
    public bool IsEditing { get; private set; }

    public bool IsViewing => !IsEditing;

    public string Initials { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Neighborhood { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public string EditName { get; set; } = string.Empty;

    public string EditPhone { get; set; } = string.Empty;

    public string EditAddress { get; set; } = string.Empty;

    public string EditError { get; private set; } = string.Empty;

    public bool HasEditError => !string.IsNullOrEmpty(EditError);

    public string DogNames { get; private set; } = string.Empty;

    /// <summary>Gets the confirmation shown after a report is written, or empty.</summary>
    public string ExportMsg { get; private set; } = string.Empty;

    public bool HasExportMsg => !string.IsNullOrEmpty(ExportMsg);

    public bool NoFuture { get; private set; }

    public ObservableCollection<TutorFutureServiceRow> FutureServices { get; } = [];

    /// <summary>
    /// Public because the View calls it from OnLoaded — see <see cref="DogDetailViewModel"/> for
    /// why OnRunStarting is not enough for a screen shown through CurrentView.
    /// </summary>
    public async Task ReloadAsync()
    {
        // Before the early returns below, so arriving here always lands on the reading state —
        // leaving mid-edit and coming back should not resume a half-finished form.
        IsEditing = false;
        EditError = string.Empty;
        ConfirmingDelete = false;
        ExportMsg = string.Empty;

        if (session.SelectedTutorId is not int tutorId)
        {
            return;
        }

        var tutor = await repositoryTutors.GetAsync(tutorId).WithSync();
        if (tutor == null)
        {
            return;
        }

        Initials = AppSession.Initials(tutor.Name);
        Name = tutor.Name;
        Neighborhood = tutor.Address ?? string.Empty;
        Phone = tutor.Telephone;

        var dogs = await repositoryDogs.ListForTutorAsync(tutorId).WithSync();
        DogNames = dogs.Length == 0 ? "Nenhum cachorro cadastrado." : string.Join(", ", dogs.Select(d => d.Name));

        var services = await repositoryServices.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();
        var dogIds = dogs.Select(d => d.DogId).ToHashSet();
        tutorServices = [.. services.Where(s => dogIds.Contains(s.DogId)).OrderBy(s => s.Date)];

        var now = DateTime.Now;
        var future = tutorServices
            .Where(s => s.Date >= now)
            .OrderBy(s => s.Date)
            .Select(s => new TutorFutureServiceRow(s.DogName, AppSession.TypeLabel(s.Kind), AppSession.DateTimeLabel(s.Date)))
            .ToArray();

        FutureServices.Clear();
        foreach (var row in future)
        {
            FutureServices.Add(row);
        }

        NoFuture = future.Length == 0;
    }

    protected override async Task OnRunStarting(Unit input) => await ReloadAsync().WithSync();

    /// <summary>
    /// Writes this tutor's whole history to a PDF: every service their dogs have had, grouped by
    /// month, with what has been paid and what is still owed for each.
    /// </summary>
    /// <remarks>
    /// Months run newest first, since a bill is usually about what just happened. Amounts come
    /// from <see cref="AppSession.ServiceTotal"/>, so a hotel stay counts its whole stay rather
    /// than one night's rate.
    /// </remarks>
    private async Task Export()
    {
        var report = new ReportDocument
        {
            Title = Name,
            Subtitle = "Serviços por mês",
            Footer = $"Gerado em {DateTime.Now.ToString("dd/MM/yyyy 'às' HH:mm", CultureInfo.InvariantCulture)} · DapperDemo",
        };

        report.Summary.Add(new ReportField("Telefone", Phone));
        if (!string.IsNullOrWhiteSpace(Neighborhood))
        {
            report.Summary.Add(new ReportField("Endereço", Neighborhood));
        }

        report.Summary.Add(new ReportField("Cachorros", DogNames));

        var months = tutorServices
            .GroupBy(s => new DateTime(s.Date.Year, s.Date.Month, 1))
            .OrderByDescending(g => g.Key);

        foreach (var month in months)
        {
            var monthName = Brazil.DateTimeFormat.GetMonthName(month.Key.Month);
            var section = new ReportSection
            {
                Heading = $"{char.ToUpper(monthName[0], Brazil)}{monthName[1..]} de {month.Key.Year.ToString(CultureInfo.InvariantCulture)}",
            };

            section.Columns.Add("Cachorro");
            section.Columns.Add("Tipo");
            section.Columns.Add("Data");
            section.Columns.Add("Valor");
            section.Columns.Add("Situação");
            section.RightAligned.Add(false);
            section.RightAligned.Add(false);
            section.RightAligned.Add(false);
            section.RightAligned.Add(true);
            section.RightAligned.Add(true);

            foreach (var service in month.OrderBy(s => s.Date))
            {
                section.Rows.Add(new ReportRow(
                    service.DogName,
                    AppSession.TypeLabel(service.Kind),
                    AppSession.DateTimeLabel(service.Date),
                    AppSession.Money(AppSession.ServiceTotal(service)),
                    service.ServicePaid ? "Pago" : "Pendente"));
            }

            var paid = month.Where(s => s.ServicePaid).Sum(AppSession.ServiceTotal);
            var pending = month.Where(s => !s.ServicePaid).Sum(AppSession.ServiceTotal);
            section.Totals.Add(new ReportField("Total do mês", AppSession.Money(paid + pending)));
            section.Totals.Add(new ReportField("Já pago", AppSession.Money(paid)));
            section.Totals.Add(new ReportField("A pagar", AppSession.Money(pending), true));
            report.Sections.Add(section);
        }

        if (report.Sections.Count == 0)
        {
            var empty = new ReportSection
            {
                Heading = "Serviços",
                EmptyMessage = "Nenhum serviço registrado para este tutor.",
            };
            report.Sections.Add(empty);
        }
        else
        {
            var allPaid = tutorServices.Where(s => s.ServicePaid).Sum(AppSession.ServiceTotal);
            var allPending = tutorServices.Where(s => !s.ServicePaid).Sum(AppSession.ServiceTotal);

            // Only the outstanding amount is emphasised. A bold grand total next to it reads as
            // the figure to pay, which would have the tutor paying for months already settled.
            var overall = new ReportSection { Heading = "Total geral" };
            overall.Totals.Add(new ReportField("Valor dos serviços", AppSession.Money(allPaid + allPending)));
            overall.Totals.Add(new ReportField("Já pago", AppSession.Money(allPaid)));
            overall.Totals.Add(new ReportField("A pagar", AppSession.Money(allPending), true));
            report.Sections.Add(overall);

            await AddPaymentSectionAsync(report, allPending).WithSync();
        }

        var slug = new string([.. Name.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-')]).Trim('-');
        var fileName = await reportExporter.ExportAsync(report, $"servicos-{slug}").WithSync();
        ExportMsg = fileName == null ? string.Empty : $"Relatório salvo: {fileName}";
    }

    /// <summary>
    /// Adds where to send the money: the pet sitter's own name and Pix key, with the outstanding
    /// amount beside them, so the tutor can pay straight from the report.
    /// </summary>
    /// <remarks>
    /// Read from the account rather than from the session, because the session only carries the
    /// name — and a Pix key edited on the profile screen has to reach the next report.
    /// </remarks>
    /// <param name="report">The report being built.</param>
    /// <param name="pending">What the tutor still owes across every month.</param>
    private async Task AddPaymentSectionAsync(ReportDocument report, decimal pending)
    {
        var petSitter = await repositoryPetSitter.GetAsync(session.CurrentPetSitterId).WithSync();

        // Nothing to tell the tutor if there is no key on file and nothing outstanding.
        if (string.IsNullOrWhiteSpace(petSitter?.Pix) && pending <= 0)
        {
            return;
        }

        var payment = new ReportSection { Heading = "Pagamento" };

        if (petSitter != null)
        {
            payment.Fields.Add(new ReportField("Favorecido", petSitter.Name));
        }

        if (!string.IsNullOrWhiteSpace(petSitter?.Pix))
        {
            payment.Fields.Add(new ReportField("Chave Pix", petSitter.Pix, true));
        }

        // The same outstanding figure as the grand total above, restated beside the key so the
        // amount and where to send it are read together.
        payment.Fields.Add(new ReportField(
            "Total a pagar",
            pending > 0 ? AppSession.Money(pending) : "Nada pendente. Obrigado!",
            pending > 0));

        report.Sections.Add(payment);
    }

    private Task StartEdit()
    {
        // Seeded from the loaded record, so cancelling and reopening the editor starts from the
        // saved values again rather than from whatever was typed and abandoned.
        EditName = Name;
        EditPhone = Phone;
        EditAddress = Neighborhood;
        EditError = string.Empty;
        IsEditing = true;
        return Task.CompletedTask;
    }

    private Task CancelEdit()
    {
        EditError = string.Empty;
        IsEditing = false;
        return Task.CompletedTask;
    }

    private async Task SaveEdit()
    {
        if (session.SelectedTutorId is not int tutorId)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(EditName))
        {
            EditError = "Informe o nome do tutor.";
            return;
        }

        if (string.IsNullOrWhiteSpace(EditPhone))
        {
            EditError = "Informe o telefone.";
            return;
        }

        var result = await repositoryTutors.Update(new Tutors
        {
            TutorId = tutorId,
            Name = EditName.Trim(),
            Telephone = EditPhone.Trim(),
            Address = EditAddress.Trim(),
        }).WithSync();

        if (result != Response.Successful)
        {
            EditError = "Não foi possível salvar as alterações.";
            return;
        }

        IsEditing = false;
        session.NotifyDataChanged();
        await ReloadAsync().WithSync();
    }

    private async Task Delete()
    {
        if (session.SelectedTutorId is not { } tutorId)
        {
            return;
        }

        // Cascades to this tutor's dogs and their services — see RepositoryTutors.Delete.
        await repositoryTutors.Delete(tutorId).WithSync();
        session.SelectedTutorId = null;
        session.NotifyDataChanged();
        currentView.GoBack();
    }
}