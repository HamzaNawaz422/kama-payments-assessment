using Payments.Application.Abstractions;
using Payments.Application.Common;

namespace Payments.Application.Reports;

public sealed class GetDailyReportHandler(IReportQueries reports)
{
    private const int MaxRangeDays = 366;

    public Task<IReadOnlyList<DailyPaymentReportRow>> HandleAsync(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var toDate = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = from ?? toDate.AddDays(-30);

        if (fromDate > toDate)
            throw new ValidationException("'from' must be on or before 'to'.");

        if (toDate.DayNumber - fromDate.DayNumber > MaxRangeDays)
            throw new ValidationException($"Date range cannot exceed {MaxRangeDays} days.");

        return reports.GetDailyPaymentReportAsync(fromDate, toDate, ct);
    }
}