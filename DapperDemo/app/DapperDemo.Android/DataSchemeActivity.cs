using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.Core.View;
using Avalonia.Android;

namespace DapperDemo.Android;

/// <summary>
/// Special activity to handle OpenUri activation.
/// `AvaloniaActivity` internally will redirect parameters to the Avalonia Application.
/// </summary>
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true, Theme = "@android:style/Theme.NoDisplay")]
// [IntentFilter(new[] { ActionView }, Categories = new[] { CategoryDefault, CategoryBrowsable }, DataScheme = "avln")]
public class DataSchemeActivity : AvaloniaActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
    }
}