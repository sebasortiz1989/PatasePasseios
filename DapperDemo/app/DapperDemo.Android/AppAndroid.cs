using Verion.Treinamento.DapperDemo.Android.DependencyInversion;
using Verion.Treinamento.DapperDemo.View;

namespace Verion.Treinamento.DapperDemo.Android;

public class AppAndroid : App
{
    public AppAndroid() : base(new DroidContainerBuilder().Build())
    {
    }
}
