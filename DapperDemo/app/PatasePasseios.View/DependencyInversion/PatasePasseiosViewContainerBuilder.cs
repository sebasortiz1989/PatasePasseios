using AvaloniaFramework.Hosting.DependencyInjection;
using PatasePasseios.View.Reports;
using PatasePasseios.View.Services;
using PatasePasseios.Viewmodel.DependencyInversion;
using AgendaView = PatasePasseios.View.Views.TabViews.AgendaView;
using DogDetailView = PatasePasseios.View.Views.ComplementaryViews.DogDetailView;
using DogsView = PatasePasseios.View.Views.TabViews.DogsView;
using LoginView = PatasePasseios.View.Views.NavigationViews.LoginView;
using MainView = PatasePasseios.View.Views.NavigationViews.MainView;
using NewDogView = PatasePasseios.View.Views.ComplementaryViews.NewDogView;
using NewTutorView = PatasePasseios.View.Views.ComplementaryViews.NewTutorView;
using ServiceDetailView = PatasePasseios.View.Views.ComplementaryViews.ServiceDetailView;
using ServicesView = PatasePasseios.View.Views.TabViews.ServicesView;
using SettingsView = PatasePasseios.View.Views.ComplementaryViews.SettingsView;
using SignUpView = PatasePasseios.View.Views.NavigationViews.SignUpView;
using TutorDetailView = PatasePasseios.View.Views.ComplementaryViews.TutorDetailView;
using TutorsView = PatasePasseios.View.Views.TabViews.TutorsView;
using UsersView = PatasePasseios.View.Views.TabViews.UsersView;

namespace PatasePasseios.View.DependencyInversion
{
    public class PatasePasseiosViewContainerBuilder : ImmutableContainerBuilder
    {
        public PatasePasseiosViewContainerBuilder()
            : base(GetBuilders())
        {
        }

        private static IEnumerable<ContainerBuilder> GetBuilders()
        {
            yield return new AvaloniaViewContainerBuilder();
            yield return new PatasePasseiosViewmodelContainerBuilder();
            yield return new ImmutableContainerBuilder(GetRegistrations());
        }

        private static IEnumerable<ContainerRegistration> GetRegistrations()
        {
            // Picking a file needs a TopLevel, which only this layer can reach — the view models
            // take the ImagePicker abstraction from PatasePasseios.Viewmodel.
            yield return CreateSingleton<StorageProviderImagePicker>().WithAbstractions();

            // Same reasoning: launching a URI needs a TopLevel, so the view models take the
            // UriLauncher abstraction and this layer supplies the Avalonia one.
            yield return CreateSingleton<AvaloniaUriLauncher>().WithAbstractions();
            yield return CreateSingleton<StorageProviderFileExportDialog>().WithAbstractions();

            // Applying a theme or a type size means writing to the running Application, which is
            // this layer's to touch — the view models take the DisplaySettings abstraction.
            yield return CreateSingleton<AvaloniaDisplaySettings>().WithAbstractions();

            // Picking and remembering a folder needs a TopLevel's storage provider, so the backup
            // destination lives here rather than in the composition root. The Google Drive store
            // will replace this one line and belongs in this layer for the same reason — its
            // sign-in needs a browser.
            yield return CreateSingleton<UserFolderBackupStore>().WithAbstractions();

            // Drawing and the save dialog both need this layer; the view models build a
            // ReportDocument and never see a control.
            yield return CreateSingleton<PngReportExporter>().WithAbstractions();

            // The share sheet the desktop heads get, which is none. The Android head registers a
            // real one after this builder and the container takes the later registration, so this
            // is the fallback rather than the answer everywhere.
            yield return CreateSingleton<UnsupportedShareSheet>().WithAbstractions();

            yield return CreateTransient<LoginView>().WithAbstractions();
            yield return CreateTransient<SignUpView>().WithAbstractions();
            yield return CreateTransient<MainView>().WithAbstractions();
            yield return CreateTransient<NewTutorView>().WithAbstractions();
            yield return CreateTransient<NewDogView>().WithAbstractions();
            yield return CreateTransient<DogDetailView>().WithAbstractions();
            yield return CreateTransient<TutorDetailView>().WithAbstractions();
            yield return CreateTransient<ServiceDetailView>().WithAbstractions();
            yield return CreateTransient<SettingsView>().WithAbstractions();
            yield return CreateTransient<DogsView>().WithAbstractions();
            yield return CreateTransient<TutorsView>().WithAbstractions();
            yield return CreateTransient<AgendaView>().WithAbstractions();
            yield return CreateTransient<UsersView>().WithAbstractions();
            yield return CreateTransient<ServicesView>().WithAbstractions();
        }
    }
}