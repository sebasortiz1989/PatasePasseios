using AvaloniaFramework.DependencyInjection;
using AvaloniaFramework.Hosting.DependencyInjection;
using DapperDemo.View.Reports;
using DapperDemo.View.Services;
using DapperDemo.View.Views;
using DapperDemo.View.Views.MainViewViews;
using DapperDemo.Viewmodel.DependencyInversion;
using System.Collections.Generic;

namespace DapperDemo.View.DependencyInversion
{
    public class DapperDemoViewContainerBuilder : ImmutableContainerBuilder
    {
        public DapperDemoViewContainerBuilder()
            : base(GetBuilders())
        {
        }

        private static IEnumerable<ContainerBuilder> GetBuilders()
        {
            yield return new AvaloniaViewContainerBuilder();
            yield return new DapperDemoViewmodelContainerBuilder();
            yield return new ImmutableContainerBuilder(GetRegistrations());
        }

        private static IEnumerable<ContainerRegistration> GetRegistrations()
        {
            // Picking a file needs a TopLevel, which only this layer can reach — the view models
            // take the ImagePicker abstraction from DapperDemo.Viewmodel.
            yield return CreateSingleton<StorageProviderImagePicker>().WithAbstractions();

            // Same reasoning: launching a URI needs a TopLevel, so the view models take the
            // UriLauncher abstraction and this layer supplies the Avalonia one.
            yield return CreateSingleton<AvaloniaUriLauncher>().WithAbstractions();
            yield return CreateSingleton<StorageProviderBackupFileDialog>().WithAbstractions();

            // Drawing and the save dialog both need this layer; the view models build a
            // ReportDocument and never see a control.
            yield return CreateSingleton<PngReportExporter>().WithAbstractions();

            yield return CreateTransient<LoginView>().WithAbstractions();
            yield return CreateTransient<SignUpView>().WithAbstractions();
            yield return CreateTransient<MainView>().WithAbstractions();
            yield return CreateTransient<NewTutorView>().WithAbstractions();
            yield return CreateTransient<NewDogView>().WithAbstractions();
            yield return CreateTransient<DogDetailView>().WithAbstractions();
            yield return CreateTransient<TutorDetailView>().WithAbstractions();
            yield return CreateTransient<ServiceDetailView>().WithAbstractions();
            yield return CreateTransient<DogsView>().WithAbstractions();
            yield return CreateTransient<TutorsView>().WithAbstractions();
            yield return CreateTransient<AgendaView>().WithAbstractions();
            yield return CreateTransient<UsersView>().WithAbstractions();
            yield return CreateTransient<ServicesView>().WithAbstractions();
        }
    }
}