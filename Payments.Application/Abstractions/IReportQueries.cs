namespace Payments.Application.Abstractions;

public interface IReportQueries
{
    /// <summary>Calls the SQL Server stored procedure sp_DailyPaymentReport.</summary>
    Task<IReadOnlyList<DailyPaymentReportRow>> GetDailyPaymentReportAsync(
        DateOnly from, DateOnly to, CancellationToken ct);
}

public sealed record DailyPaymentReportRow(
    DateOnly Day, string ProviderName, string Status, int PaymentCount, decimal TotalAmount, string Currency);