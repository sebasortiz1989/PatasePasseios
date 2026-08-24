using PatasePasseios.iOS.DependencyInversion;
using PatasePasseios.View;

namespace PatasePasseios.iOS;

internal sealed class AppIphone : App
{
    public AppIphone()
        : base(new IPhoneContainerBuilder().Build())
    {
    }
}