using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyPaymentReportProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
        CREATE OR ALTER PROCEDURE dbo.sp_DailyPaymentReport
            @FromDate DATE,
            @ToDate   DATE
        AS
        BEGIN
            SET NOCOUNT ON;

            SELECT
                CAST(p.CreatedAtUtc AS DATE) AS [Day],
                pr.Name                      AS ProviderName,
                p.Status                     AS [Status],
                COUNT(*)                     AS PaymentCount,
                SUM(p.Amount)                AS TotalAmount,
                p.Currency                   AS Currency
            FROM dbo.Payments p
            INNER JOIN dbo.PaymentProviders pr ON pr.Id = p.ProviderId
            WHERE p.CreatedAtUtc >= @FromDate
              AND p.CreatedAtUtc <  DATEADD(DAY, 1, @ToDate)
            GROUP BY CAST(p.CreatedAtUtc AS DATE), pr.Name, p.Status, p.Currency
            ORDER BY [Day], ProviderName, [Status];
        END
        """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_DailyPaymentReport;");
        }
    }
}
