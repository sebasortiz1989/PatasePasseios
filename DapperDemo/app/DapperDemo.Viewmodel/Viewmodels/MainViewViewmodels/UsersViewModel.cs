using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using PropertyChanged;
using Verion.Presentation.View;
using Verion.Presentation.View.UseCase;
using Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.Mock;

namespace Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.MainViewViewmodels;

public class IncomeRow(string label, string amount)
{
    public string Label { get; } = label;

    public string Amount { get; } = amount;
}

[AddINotifyPropertyChangedInterface]
public class UsersViewModel : PresentationModelBase<Void, Void>
{
    private readonly MockAppData mockAppData;
    private readonly EventHandler dataChangedHandler;

    public UsersViewModel(MockAppData mockAppData)
    {
        this.mockAppData = mockAppData;

        // Billing totals depend on services marked paid elsewhere (Agenda, service detail).
        dataChangedHandler = (_, _) => Refresh();
        mockAppData.DataChanged += dataChangedHandler;

        OpenPasswordFormCommand = new SynchronizedCommand(OpenPasswordForm, SynchronizationBehavior.Discard, true);
        CancelPasswordFormCommand = new SynchronizedCommand(() => ShowPasswordForm = false, SynchronizationBehavior.Discard, true);
        SavePasswordCommand = new SynchronizedCommand(SavePassword, SynchronizationBehavior.Discard, true);
        LogoutCommand = new SynchronizedCommand(mockAppData.RequestLogout, SynchronizationBehavior.Discard, true);

        CurrentMonthLabel = DateTime.Now.ToString("MMMM 'de' yyyy", new CultureInfo("pt-BR"));
        Refresh();
    }

    public ICommand OpenPasswordFormCommand { get; }

    public ICommand CancelPasswordFormCommand { get; }

    public ICommand SavePasswordCommand { get; }

    public ICommand LogoutCommand { get; }

    public string CurrentUserName { get; set; } = string.Empty;

    public bool ShowPasswordForm { get; set; }

    public bool ShowPasswordSummary => !ShowPasswordForm;

    public string NewPw { get; set; } = string.Empty;

    public string ConfirmPw { get; set; } = string.Empty;

    public string PwMsg { get; set; } = string.Empty;

    public bool HasPwMsg => !string.IsNullOrEmpty(PwMsg);

    public string CurrentMonthLabel { get; }

    public string MonthTotalLabel { get; set; } = string.Empty;

    public ObservableCollection<IncomeRow> IncomeBreakdown { get; } = [];

    protected override Task OnRunStarting(Void input)
    {
        Refresh();
        return Task.CompletedTask;
    }

    protected override Task OnRunFinishing()
    {
        mockAppData.DataChanged -= dataChangedHandler;
        return Task.CompletedTask;
    }

    private void OpenPasswordForm()
    {
        NewPw = string.Empty;
        ConfirmPw = string.Empty;
        PwMsg = string.Empty;
        ShowPasswordForm = true;
    }

    private void SavePassword()
    {
        if (string.IsNullOrEmpty(NewPw) || NewPw.Length < 4)
        {
            PwMsg = "A senha deve ter ao menos 4 caracteres.";
            return;
        }

        if (NewPw != ConfirmPw)
        {
            PwMsg = "As senhas não coincidem.";
            return;
        }

        mockAppData.ChangePassword(NewPw);
        PwMsg = string.Empty;
        ShowPasswordForm = false;
    }

    private void Refresh()
    {
        CurrentUserName = mockAppData.CurrentUserName;

        var now = DateTime.Now;
        decimal walk = 0, sitting = 0, hotel = 0;

        foreach (var service in mockAppData.Services)
        {
            if (!service.Paid || service.Date.Month != now.Month || service.Date.Year != now.Year)
            {
                continue;
            }

            switch (service.Kind)
            {
                case ServiceKind.Walk:
                    walk += service.Price ?? 0;
                    break;
                case ServiceKind.Sitting:
                    sitting += service.Price ?? 0;
                    break;
                case ServiceKind.Hotel:
                    hotel += service.PricePerDay ?? 0;
                    break;
            }
        }

        MonthTotalLabel = MockAppData.Money(walk + sitting + hotel);

        IncomeBreakdown.Clear();
        IncomeBreakdown.Add(new IncomeRow("Passeio", MockAppData.Money(walk)));
        IncomeBreakdown.Add(new IncomeRow("Pet sitting", MockAppData.Money(sitting)));
        IncomeBreakdown.Add(new IncomeRow("Hotel", MockAppData.Money(hotel)));
    }
}
