using AvaloniaFramework.DependencyInjection;
using PatasePasseios.Viewmodel.Viewmodels;
using PatasePasseios.Viewmodel.Viewmodels.ComplementaryViewsViewmodels;
using PatasePasseios.Viewmodel.Viewmodels.NavigationViewsViewmodels;
using PatasePasseios.Viewmodel.Viewmodels.Session;
using PatasePasseios.Viewmodel.Viewmodels.TabViewsViewmodels;
using PatasePasseios.Viewmodel.Viewmodels.Utils;

namespace PatasePasseios.Viewmodel.DependencyInversion
{
    public sealed class PatasePasseiosViewmodelContainerBuilder : ImmutableContainerBuilder
    {
        public PatasePasseiosViewmodelContainerBuilder()
            : base(GetRegistrations())
        {
        }

        private static IEnumerable<ContainerRegistration> GetRegistrations()
        {
            yield return CreateSingleton<AppSession>();
            yield return CreateSingleton<CurrentView>();
            yield return CreateSingleton<CreditSpender>();
            yield return CreateTransient<LoginViewModel>().WithAbstractions();
            yield return CreateTransient<SignUpViewModel>().WithAbstractions();
            yield return CreateTransient<MainViewModel>().WithAbstractions();
            yield return CreateTransient<NewTutorViewModel>().WithAbstractions();
            yield return CreateTransient<NewDogViewModel>().WithAbstractions();
            yield return CreateTransient<DogDetailViewModel>().WithAbstractions();
            yield return CreateTransient<TutorDetailViewModel>().WithAbstractions();
            yield return CreateTransient<ServiceDetailViewModel>().WithAbstractions();
            yield return CreateTransient<SettingsViewModel>().WithAbstractions();
            yield return CreateTransient<DogsViewModel>().WithAbstractions();
            yield return CreateTransient<TutorsViewModel>().WithAbstractions();
            yield return CreateTransient<AgendaViewModel>().WithAbstractions();
            yield return CreateTransient<UsersViewModel>().WithAbstractions();
            yield return CreateTransient<ServicesViewModel>().WithAbstractions();
        }
    }
}