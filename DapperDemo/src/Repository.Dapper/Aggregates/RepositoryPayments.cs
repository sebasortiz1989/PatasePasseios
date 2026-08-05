using Dapper;
using DapperDemo.Repository.Dapper.Dtos;
using DapperDemo.Repository.Dapper.Services;
using Microsoft.Data.Sqlite;

namespace DapperDemo.Repository.Dapper.Aggregates;

/// <summary>
/// The ledger of money received from tutors, and the only place a payment is written or unwound.
/// </summary>
/// <remarks>
/// <para>
/// A payment is three writes at once — settle the services it covers, bank whatever is left as
/// credit, and record that both came from one amount — so all three live here in one transaction
/// rather than being sequenced by a view model.
/// </para>
/// <para>
/// It spans the four service tables and the Tutors table, like
/// <see cref="RepositoryServices"/> and unlike <see cref="RepositoryBase{TEntity}"/>, so it derives
/// from neither.
/// </para>
/// </remarks>
public sealed class RepositoryPayments(DapperDatabaseService dapperDatabaseService, RepositoryServices repositoryServices)
{
    private DapperDatabaseService DapperDatabaseService { get; } = dapperDatabaseService;

    private RepositoryServices RepositoryServices { get; } = repositoryServices;

    /// <summary>
    /// Every payment this tutor has made, newest first, each carrying what it settled.
    /// </summary>
    /// <param name="tutorId">The tutor whose payments to read.</param>
    /// <returns>The tutor's payments, or an empty list when there are none.</returns>
    public async Task<TutorPayment[]> ListForTutorAsync(int tutorId)
    {
        try
        {
            using var connection = DapperDatabaseService.Connection;
            await connection.OpenAsync().ConfigureAwait(false);

            var headers = await connection.QueryAsync<TutorPayment>(
                sql: """
                     SELECT TutorPaymentId, TutorId, PetSitterId, Date, Amount, CreditStored
                     FROM TutorPayments
                     WHERE TutorId = @TutorId
                     ORDER BY Date DESC, TutorPaymentId DESC
                     """,
                param: new { TutorId = tutorId }).ConfigureAwait(false);

            var payments = headers.ToArray();
            if (payments.Length == 0)
            {
                return [];
            }

            var allocations = await connection.QueryAsync<AllocationRow>(
                sql: """
                     SELECT a.TutorPaymentId, a.Kind, a.ServiceId, a.Amount
                     FROM TutorPaymentAllocations a
                     INNER JOIN TutorPayments p ON p.TutorPaymentId = a.TutorPaymentId
                     WHERE p.TutorId = @TutorId
                     """,
                param: new { TutorId = tutorId }).ConfigureAwait(false);

            var byPayment = new Dictionary<int, IReadOnlyList<ServicePayment>>();
            foreach (var group in allocations.GroupBy(a => a.TutorPaymentId))
            {
                byPayment[group.Key] = [.. group.Select(Allocation)];
            }

            return [.. payments.Select(p => new TutorPayment
            {
                TutorPaymentId = p.TutorPaymentId,
                TutorId = p.TutorId,
                PetSitterId = p.PetSitterId,
                Date = p.Date,
                Amount = p.Amount,
                CreditStored = p.CreditStored,
                Allocations = byPayment.TryGetValue(p.TutorPaymentId, out var found) ? found : [],
            })];
        }
        catch (SqliteException e)
        {
            // The tables are missing only on a database restored from a backup taken before the
            // ledger existed; the next launch creates them. An empty history beats a crash.
            Console.WriteLine(e);
            return [];
        }
    }

    /// <summary>
    /// Writes a payment: the ledger entry, the services it settles, and any remainder held as credit.
    /// </summary>
    /// <remarks>
    /// One transaction because a payment is one event. The split across services is decided by the
    /// caller through <see cref="PaymentAllocation"/>; what is not spread there is an advance, and
    /// lands on the tutor's credit. Each service's settled total is added to rather than assigned,
    /// since one service may be settled more than once, and its price is never touched.
    /// </remarks>
    /// <param name="payment">The amount received, with the allocation already worked out.</param>
    /// <returns>Whether the write succeeded.</returns>
    public async Task<Response> RecordAsync(TutorPayment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);

        try
        {
            using var connection = DapperDatabaseService.Connection;
            await connection.OpenAsync().ConfigureAwait(false);
            using var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);

            var paymentId = await connection.ExecuteScalarAsync<int>(
                sql: """
                     INSERT INTO TutorPayments (TutorId, PetSitterId, Date, Amount, CreditStored)
                     VALUES (@TutorId, @PetSitterId, @Date, @Amount, @CreditStored);
                     SELECT last_insert_rowid();
                     """,
                param: new { payment.TutorId, payment.PetSitterId, payment.Date, payment.Amount, payment.CreditStored },
                transaction: transaction).ConfigureAwait(false);

            foreach (var allocation in payment.Allocations)
            {
                var (table, key) = ServiceTables.For(allocation.Kind);

                await connection.ExecuteAsync(
                    sql: """
                         INSERT INTO TutorPaymentAllocations (TutorPaymentId, Kind, ServiceId, Amount)
                         VALUES (@TutorPaymentId, @Kind, @ServiceId, @Amount)
                         """,
                    param: new
                    {
                        TutorPaymentId = paymentId,
                        Kind = (int)allocation.Kind,
                        allocation.ServiceId,
                        allocation.Amount,
                    },
                    transaction: transaction).ConfigureAwait(false);

                await connection.ExecuteAsync(
                    sql: $"""
                          UPDATE {table}
                          SET AmountSettled = AmountSettled + @Amount,
                              CreditApplied = CreditApplied + @FromCredit,
                              ServicePaid = @FullyPaid
                          WHERE {key} = @ServiceId
                          """,
                    param: new { allocation.Amount, allocation.FromCredit, allocation.FullyPaid, allocation.ServiceId },
                    transaction: transaction).ConfigureAwait(false);
            }

            if (payment.CreditStored > 0m)
            {
                await connection.ExecuteAsync(
                    sql: "UPDATE Tutors SET Credit = Credit + @CreditStored WHERE TutorId = @TutorId",
                    param: new { payment.CreditStored, payment.TutorId },
                    transaction: transaction).ConfigureAwait(false);
            }

            await transaction.CommitAsync().ConfigureAwait(false);
            return Response.Successful;
        }
        catch (SqliteException e)
        {
            Console.WriteLine(e);
            return Response.Failed;
        }
    }

    /// <summary>
    /// Takes a payment back out: unsettles the services it covered and reclaims what it left as credit.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The settled amounts are recomputed rather than decremented in SQL, so a service that was
    /// settled twice keeps the other payment's share, and whether it is still fully paid is decided
    /// from its own total instead of from a flag written when this payment was taken.
    /// </para>
    /// <para>
    /// Credit is the awkward half. What the payment banked may already have been spent on later
    /// bookings, in which case the tutor no longer holds it — so anything the balance cannot cover
    /// is followed into the services it went to, newest first, and taken back from there. Without
    /// that, deleting an advance would leave services paid for with money that never existed.
    /// </para>
    /// </remarks>
    /// <param name="tutorPaymentId">The payment to undo.</param>
    /// <returns>Whether the write succeeded.</returns>
    public async Task<Response> DeleteAsync(int tutorPaymentId)
    {
        var payment = await GetAsync(tutorPaymentId).ConfigureAwait(false);
        if (payment == null)
        {
            return Response.Failed;
        }

        var services = await RepositoryServices.ListForTutorAsync(payment.PetSitterId, payment.TutorId).ConfigureAwait(false);
        var balances = services.ToDictionary(
            s => (s.Kind, s.ServiceId),
            s => new Balance(s.AmountSettled, s.CreditApplied, s.Total));

        // A service deleted since the payment was taken is simply not here any more; its share of
        // the money went with the row.
        foreach (var allocation in payment.Allocations)
        {
            if (balances.TryGetValue((allocation.Kind, allocation.ServiceId), out var balance))
            {
                balances[(allocation.Kind, allocation.ServiceId)] = balance with
                {
                    Settled = Math.Max(balance.Settled - allocation.Amount, 0m),
                };
            }
        }

        var heldCredit = await ReadCreditAsync(payment.TutorId).ConfigureAwait(false);
        var fromBalance = Math.Min(heldCredit, payment.CreditStored);
        var shortfall = payment.CreditStored - fromBalance;

        // Newest first: the most recent booking is the one the credit most likely reached, and
        // unwinding from the back leaves the older history alone.
        foreach (var service in services.OrderByDescending(s => s.Date).ThenByDescending(s => s.ServiceId))
        {
            if (shortfall <= 0m)
            {
                break;
            }

            var balance = balances[(service.Kind, service.ServiceId)];
            var reclaimed = Math.Min(shortfall, balance.Credit);
            if (reclaimed <= 0m)
            {
                continue;
            }

            shortfall -= reclaimed;
            balances[(service.Kind, service.ServiceId)] = balance with
            {
                Settled = Math.Max(balance.Settled - reclaimed, 0m),
                Credit = balance.Credit - reclaimed,
            };
        }

        try
        {
            using var connection = DapperDatabaseService.Connection;
            await connection.OpenAsync().ConfigureAwait(false);
            using var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);

            foreach (var service in services)
            {
                var original = new Balance(service.AmountSettled, service.CreditApplied, service.Total);
                var balance = balances[(service.Kind, service.ServiceId)];
                if (balance == original)
                {
                    continue;
                }

                var (table, key) = ServiceTables.For(service.Kind);

                await connection.ExecuteAsync(
                    sql: $"""
                          UPDATE {table}
                          SET AmountSettled = @Settled,
                              CreditApplied = @Credit,
                              ServicePaid = @Paid
                          WHERE {key} = @ServiceId
                          """,
                    param: new
                    {
                        balance.Settled,
                        balance.Credit,

                        // Recomputed, not cleared: another payment may still cover this service in
                        // full. Only services this reversal touched are rewritten, so a booking
                        // ticked paid by hand is never quietly un-ticked.
                        Paid = balance.Total > 0m && balance.Settled >= balance.Total,
                        service.ServiceId,
                    },
                    transaction: transaction).ConfigureAwait(false);
            }

            if (payment.CreditStored > 0m)
            {
                await connection.ExecuteAsync(
                    sql: "UPDATE Tutors SET Credit = MAX(Credit - @CreditStored, 0) WHERE TutorId = @TutorId",
                    param: new { payment.CreditStored, payment.TutorId },
                    transaction: transaction).ConfigureAwait(false);
            }

            await connection.ExecuteAsync(
                sql: """
                     DELETE FROM TutorPaymentAllocations WHERE TutorPaymentId = @TutorPaymentId;
                     DELETE FROM TutorPayments WHERE TutorPaymentId = @TutorPaymentId;
                     """,
                param: new { TutorPaymentId = tutorPaymentId },
                transaction: transaction).ConfigureAwait(false);

            await transaction.CommitAsync().ConfigureAwait(false);
            return Response.Successful;
        }
        catch (SqliteException e)
        {
            Console.WriteLine(e);
            return Response.Failed;
        }
    }

    /// <summary>One payment with its allocations, or null when it is not there.</summary>
    /// <param name="tutorPaymentId">The payment to read.</param>
    /// <returns>The payment, or null.</returns>
    public async Task<TutorPayment?> GetAsync(int tutorPaymentId)
    {
        try
        {
            using var connection = DapperDatabaseService.Connection;
            await connection.OpenAsync().ConfigureAwait(false);

            var payment = await connection.QueryFirstOrDefaultAsync<TutorPayment>(
                sql: """
                     SELECT TutorPaymentId, TutorId, PetSitterId, Date, Amount, CreditStored
                     FROM TutorPayments
                     WHERE TutorPaymentId = @TutorPaymentId
                     """,
                param: new { TutorPaymentId = tutorPaymentId }).ConfigureAwait(false);

            if (payment == null)
            {
                return null;
            }

            var allocations = await connection.QueryAsync<AllocationRow>(
                sql: """
                     SELECT TutorPaymentId, Kind, ServiceId, Amount
                     FROM TutorPaymentAllocations
                     WHERE TutorPaymentId = @TutorPaymentId
                     """,
                param: new { TutorPaymentId = tutorPaymentId }).ConfigureAwait(false);

            return new TutorPayment
            {
                TutorPaymentId = payment.TutorPaymentId,
                TutorId = payment.TutorId,
                PetSitterId = payment.PetSitterId,
                Date = payment.Date,
                Amount = payment.Amount,
                CreditStored = payment.CreditStored,
                Allocations = [.. allocations.Select(Allocation)],
            };
        }
        catch (SqliteException e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    /// <summary>
    /// Turns a stored allocation back into the shape the rest of the money rules speak in.
    /// </summary>
    /// <remarks>
    /// FullyPaid is false and FromCredit zero because neither is persisted: a reversal works out
    /// for itself whether the service is still covered, and cash payments never draw on credit.
    /// </remarks>
    /// <param name="row">The stored row.</param>
    /// <returns>The allocation.</returns>
    private static ServicePayment Allocation(AllocationRow row) =>
        new((ServiceKind)row.Kind, row.ServiceId, row.Amount, fullyPaid: false);

    private async Task<decimal> ReadCreditAsync(int tutorId)
    {
        using var connection = DapperDatabaseService.Connection;
        await connection.OpenAsync().ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<decimal>(
            sql: "SELECT Credit FROM Tutors WHERE TutorId = @TutorId",
            param: new { TutorId = tutorId }).ConfigureAwait(false);
    }

    /// <summary>One row of TutorPaymentAllocations, as Dapper reads it.</summary>
    private sealed class AllocationRow
    {
        public int TutorPaymentId { get; init; }

        public int Kind { get; init; }

        public int ServiceId { get; init; }

        public decimal Amount { get; init; }
    }

    /// <summary>What one service has had settled against it while a reversal is being worked out.</summary>
    /// <param name="Settled">How much of the service has been paid for, by cash or credit.</param>
    /// <param name="Credit">How much of <paramref name="Settled"/> came from credit.</param>
    /// <param name="Total">What the service costs, which decides whether it is still fully paid.</param>
    private sealed record Balance(decimal Settled, decimal Credit, decimal Total);
}