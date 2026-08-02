using System.Collections.Generic;
using AvaloniaFramework.Hosting.DependencyInjection;
using AvaloniaFramework.DependencyInjection;
using DapperDemo.View.Views;
using DapperDemo.View.Views.MainViewViews;
using DapperDemo.Viewmodel.DependencyInversion;

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
            yield return CreateTransient<HomeView>().WithAbstractions();
            yield return CreateTransient<UsersView>().WithAbstractions();
            yield return CreateTransient<ServicesView>().WithAbstractions();
        }
    }
}