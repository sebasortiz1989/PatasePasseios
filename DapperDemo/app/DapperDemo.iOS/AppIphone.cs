using Verion.Treinamento.DapperDemo.iOS.DependencyInversion;
using Verion.Treinamento.DapperDemo.View;

namespace Verion.Treinamento.DapperDemo.iOS;

internal sealed class AppIphone: App
{
    public AppIphone() : base(new IPhoneContainerBuilder().Build())
    {
    }
}