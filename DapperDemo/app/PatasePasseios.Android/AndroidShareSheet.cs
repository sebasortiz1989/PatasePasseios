using Android.Content;
using AndroidX.Core.Content;
using PatasePasseios.Repository.Dapper;
using PatasePasseios.Viewmodel.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using Application = Android.App.Application;
using Uri = Android.Net.Uri;

namespace PatasePasseios.Android;

/// <summary>
/// Opens Android's share sheet for a rendered report, so it can go straight to WhatsApp or e-mail
/// without being saved first.
/// </summary>
/// <remarks>
/// <para>
/// Registered by <c>DroidContainerBuilder</c> after the View layer's registrations, which is what
/// replaces <c>UnsupportedShareSheet</c> on this head — the framework's container takes the later
/// registration for a service type.
/// </para>
/// <para>
/// The file is handed over as a <c>content://</c> URI from a FileProvider, never as a
/// <c>file://</c> path: since API 24 passing a file URI across an intent throws
/// <c>FileUriExposedException</c>. The provider and the folder it may address are declared in
/// AndroidManifest.xml and Resources/xml/file_paths.xml, and the two have to agree with
/// <see cref="SharedFolderName"/> below.
/// </para>
/// </remarks>
public sealed class AndroidShareSheet : ShareSheet
{
    /// <summary>
    /// The folder inside the app's cache that the FileProvider is allowed to address.
    /// </summary>
    /// <remarks>
    /// Must match the <c>path</c> in Resources/xml/file_paths.xml. The report is copied here rather
    /// than shared from wherever it was rendered: <see cref="Path.GetTempPath"/> is the cache
    /// directory on Android today, but the provider throws if handed a file outside its declared
    /// paths, and copying a megabyte is cheaper than depending on that staying true.
    /// </remarks>
    private const string SharedFolderName = "shared";

    /// <inheritdoc/>
    public bool CanShare => true;

    /// <inheritdoc/>
    public Task<Response> ShareFileAsync(string filePath, string title)
    {
        var context = Application.Context;

        try
        {
            // These are handles onto Java objects, disposed at the end of the block. The intent has
            // been marshalled across by the time StartActivity returns, so releasing the managed
            // side afterwards does not disturb the activity that was started.
            using var shared = new Java.IO.File(context.CacheDir, SharedFolderName);
            shared.Mkdirs();

            using var target = new Java.IO.File(shared, Path.GetFileName(filePath));
            File.Copy(filePath, target.AbsolutePath, true);

            // The authority is read off the running package rather than written out, so it cannot
            // drift from the ${applicationId} the manifest substitutes.
            var authority = context.PackageName + ".fileprovider";
            var uri = FileProvider.GetUriForFile(context, authority, target);

            using var intent = new Intent(Intent.ActionSend);
            intent.SetType("image/png");
            intent.PutExtra(Intent.ExtraStream, uri);

            // The grant travels with the intent, so the receiving app can read the one file for as
            // long as it needs it and nothing else.
            intent.AddFlags(ActivityFlags.GrantReadUriPermission);

            using var chooser = Intent.CreateChooser(intent, title);

            // Started from the application context rather than an activity, which Android requires
            // to be its own task.
            chooser?.AddFlags(ActivityFlags.NewTask);
            context.StartActivity(chooser);

            return Task.FromResult(Response.Successful);
        }
        catch (Java.Lang.IllegalArgumentException e)
        {
            // The provider was handed a file outside its declared paths — the manifest, the
            // file_paths resource and SharedFolderName have fallen out of step.
            Console.WriteLine(e);
            return Task.FromResult(Response.Failed);
        }
        catch (ActivityNotFoundException e)
        {
            // A device with nothing installed that accepts an image.
            Console.WriteLine(e);
            return Task.FromResult(Response.Failed);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine(e);
            return Task.FromResult(Response.Failed);
        }
    }
}