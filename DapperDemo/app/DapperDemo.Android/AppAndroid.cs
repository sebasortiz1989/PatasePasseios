using DapperDemo.Android.DependencyInversion;
using DapperDemo.View;

namespace DapperDemo.Android;

public class AppAndroid : App
{
    public AppAndroid()
        : base(new DroidContainerBuilder().Build())
    {
    }
}