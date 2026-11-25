using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payments.Application.Abstractions;
using Payments.Domain.PaymentAggregate;
using Payments.Domain.ProviderAggregate;
using Payments.Infrastructure.Persistence;
using Payments.Infrastructure.Persistence.Queries;
using Payments.Infrastructure.Persistence.Repositories;

namespace Payments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PaymentsDb")
            ?? throw new InvalidOperationException("Connection string 'PaymentsDb' is missing.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentProviderRepository, PaymentProviderRepository>();
        services.AddScoped<IPaymentQueries, PaymentQueries>();
        services.AddScoped<IReportQueries, ReportQueries>();

        return services;
    }
}