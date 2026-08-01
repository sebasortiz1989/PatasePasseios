using Verion.Infraestrutura.Dependency;
using Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels;
using Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.MainViewViewmodels;
using Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.Mock;

namespace Verion.Treinamento.DapperDemo.Viewmodel.DependencyInversion
{
    public sealed class DapperDemoViewmodelContainerBuilder : ImmutableContainerBuilder
    {
        public DapperDemoViewmodelContainerBuilder()
            : base(GetRegistrations())
        {
        }

        private static IEnumerable<ContainerRegistration> GetRegistrations()
        {
            yield return CreateSingleton<MockAppData>();
            yield return CreateTransient<LoginViewModel>().WithAbstractions();
            yield return CreateTransient<SignUpViewModel>().WithAbstractions();
            yield return CreateTransient<MainViewModel>().WithAbstractions();
            yield return CreateTransient<DogDetailViewModel>().WithAbstractions();
            yield return CreateTransient<TutorDetailViewModel>().WithAbstractions();
            yield return CreateTransient<ServiceDetailViewModel>().WithAbstractions();
            yield return CreateTransient<DogsViewModel>().WithAbstractions();
            yield return CreateTransient<TutorsViewModel>().WithAbstractions();
            yield return CreateTransient<HomeViewModel>().WithAbstractions();
            yield return CreateTransient<UsersViewModel>().WithAbstractions();
            yield return CreateTransient<ServicesViewModel>().WithAbstractions();
        }
    }
}