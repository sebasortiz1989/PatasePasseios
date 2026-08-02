using DapperDemo.View;
using DapperDemo.iOS.DependencyInversion;

namespace DapperDemo.iOS;

internal sealed class AppIphone : App
{
    public AppIphone()
        : base(new IPhoneContainerBuilder().Build())
    {
    }
}