using AvaloniaFramework.Threading;
using DapperDemo.Repository.Dapper;
using DapperDemo.Repository.Dapper.Aggregates;
using DapperDemo.Viewmodel.Viewmodels.Session;

namespace DapperDemo.Viewmodel.Viewmodels.Utils;

/// <summary>
/// Spends a tutor's credit against work that has now been carried out.
/// </summary>
/// <remarks>
/// <para>
/// Credit is money the tutor handed over beyond what was chargeable at the time — an advance on
/// work not yet done. It is spent the moment a service is <em>booked</em>: the tutor already
/// handed the money over, so putting it against a new booking is bookkeeping, not billing. The
/// executed-before-paid rule stops the sitter *asking* for money too early; it does not stop them
/// recording money they already hold, which is why this uses
/// <see cref="PaymentAllocation.AllocateCredit"/> rather than the chargeable-only rule.
/// </para>
/// <para>
/// A DI singleton rather than a static helper because it needs three repositories; registered in
/// DapperDemoViewmodelContainerBuilder.
/// </para>
/// </remarks>
public sealed class CreditSpender(
    RepositoryTutors repositoryTutors,
    RepositoryDogs repositoryDogs,
    RepositoryServices repositoryServices)
{
    private RepositoryTutors RepositoryTutors { get; } = repositoryTutors;

    private RepositoryDogs RepositoryDogs { get; } = repositoryDogs;

    private RepositoryServices RepositoryServices { get; } = repositoryServices;

    /// <summary>
    /// Settles what it can of the owning tutor's credit against their chargeable services.
    /// </summary>
    /// <param name="petSitterId">The signed-in account, which scopes the service read.</param>
    /// <param name="dogId">The dog whose service was just marked done; its tutor holds the credit.</param>
    /// <returns>A sentence describing what was spent, or empty when there was nothing to spend.</returns>
    public async Task<string> SpendForDogAsync(int petSitterId, int dogId)
    {
        var dog = await RepositoryDogs.GetAsync(dogId).NoSync();
        return dog == null ? string.Empty : await SpendAsync(petSitterId, dog.TutorId).NoSync();
    }

    /// <summary>
    /// Settles what it can of a tutor's credit against their chargeable services.
    /// </summary>
    /// <remarks>
    /// Spread oldest service first, each one keeping its price and recording what the credit
    /// covered. Anything left stays on the tutor for the next service booked.
    /// </remarks>
    /// <param name="petSitterId">The signed-in account, which scopes the service read.</param>
    /// <param name="tutorId">The tutor whose credit is being spent.</param>
    /// <returns>A sentence describing what was spent, or empty when there was nothing to spend.</returns>
    public async Task<string> SpendAsync(int petSitterId, int tutorId)
    {
        var tutor = await RepositoryTutors.GetAsync(tutorId).NoSync();
        if (tutor is not { Credit: > 0m })
        {
            return string.Empty;
        }

        var dogs = await RepositoryDogs.ListForTutorAsync(tutorId).NoSync();
        var dogIds = dogs.Select(d => d.DogId).ToHashSet();
        var services = await RepositoryServices.ListForPetSitterAsync(petSitterId).NoSync();
        var chargeable = services.Where(s => dogIds.Contains(s.DogId)).ToArray();

        var (payments, applied) = PaymentAllocation.AllocateCredit(chargeable, tutor.Credit);
        if (payments.Count == 0 || applied <= 0m)
        {
            return string.Empty;
        }

        if (await RepositoryServices.RegisterPaymentAsync(payments).NoSync() != Response.Successful)
        {
            return string.Empty;
        }

        var left = tutor.Credit - applied;
        await RepositoryTutors.SetCreditAsync(tutorId, left).NoSync();

        return left > 0m
            ? $" Crédito de {AppSession.Money(applied)} usado; restam {AppSession.Money(left)}."
            : $" Crédito de {AppSession.Money(applied)} usado.";
    }
}