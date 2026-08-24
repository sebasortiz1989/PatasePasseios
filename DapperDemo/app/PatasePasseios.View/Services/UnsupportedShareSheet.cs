using PatasePasseios.Repository.Dapper;
using PatasePasseios.Viewmodel.Services;
using System.Threading.Tasks;

namespace PatasePasseios.View.Services;

/// <summary>
/// The share sheet on the heads that have none. Desktop saves; it does not send.
/// </summary>
/// <remarks>
/// Registered by the View layer so every head resolves a <see cref="ShareSheet"/>, and replaced by
/// the Android head with a real one — the framework's container takes the later registration for a
/// service type. <see cref="CanShare"/> is false, so the screens hide the button rather than
/// offering one that reports a failure when pressed.
/// </remarks>
public sealed class UnsupportedShareSheet : ShareSheet
{
    /// <inheritdoc/>
    public bool CanShare => false;

    /// <inheritdoc/>
    public Task<Response> ShareFileAsync(string filePath, string title) => Task.FromResult(Response.Failed);
}