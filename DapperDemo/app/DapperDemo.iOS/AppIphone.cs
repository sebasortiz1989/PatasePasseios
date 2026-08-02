using DapperDemo.iOS.DependencyInversion;
using DapperDemo.View;

namespace DapperDemo.iOS;

internal sealed class AppIphone : App
{
    public AppIphone()
        : base(new IPhoneContainerBuilder().Build())
    {
    }
}