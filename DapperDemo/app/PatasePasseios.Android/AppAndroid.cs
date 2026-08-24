using PatasePasseios.Android.DependencyInversion;
using PatasePasseios.View;

namespace PatasePasseios.Android;

public class AppAndroid : App
{
    public AppAndroid()
        : base(new DroidContainerBuilder().Build())
    {
    }
}