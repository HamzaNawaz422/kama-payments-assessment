using Microsoft.EntityFrameworkCore;
using Payments.Application.Abstractions;

namespace Payments.Infrastructure.Persistence.Queries;

public sealed class ReportQueries(AppDbContext db) : IReportQueries
{
    public async Task<IReadOnlyList<DailyPaymentReportRow>> GetDailyPaymentReportAsync(
        DateOnly from, DateOnly to, CancellationToken ct)
    {
        var fromDate = from.ToDateTime(TimeOnly.MinValue);
        var toDate = to.ToDateTime(TimeOnly.MinValue);

        var rows = await db.Database
            .SqlQuery<ReportRow>($"EXEC dbo.sp_DailyPaymentReport @FromDate = {fromDate}, @ToDate = {toDate}")
            .ToListAsync(ct);

        return rows
            .Select(r => new DailyPaymentReportRow(
                DateOnly.FromDateTime(r.Day), r.ProviderName, r.Status, r.PaymentCount, r.TotalAmount, r.Currency))
            .ToList();
    }

    private sealed class ReportRow
    {
        public DateTime Day { get; set; }
        public string ProviderName { get; set; } = default!;
        public string Status { get; set; } = default!;
        public int PaymentCount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = default!;
    }
}