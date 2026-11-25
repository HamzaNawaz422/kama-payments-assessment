using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payments.Application.Abstractions;
using Payments.Domain.PaymentAggregate;
using Payments.Domain.ProviderAggregate;
using Payments.Infrastructure.Persistence;
using Payments.Infrastructure.Persistence.Queries;
using Payments.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Http.Resilience;
using Payments.Infrastructure.ExternalServices;
using Polly;
using Payments.Infrastructure.BackgroundJobs;

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

        var providerOptions = configuration.GetSection(PaymentProviderOptions.SectionName).Get<PaymentProviderOptions>()
    ?? throw new InvalidOperationException("PaymentProvider configuration is missing.");

        services.AddHttpClient<IPaymentProviderClient, PaymentProviderClient>(client =>
        {
            client.BaseAddress = new Uri(providerOptions.BaseUrl);
        })
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = providerOptions.MaxRetryAttempts;
                options.Retry.BackoffType = DelayBackoffType.Exponential;
                options.Retry.UseJitter = true;
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(providerOptions.AttemptTimeoutSeconds);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(providerOptions.TotalTimeoutSeconds);
            });

        var jobOptions = configuration.GetSection(BackgroundJobOptions.SectionName).Get<BackgroundJobOptions>()
                 ?? new BackgroundJobOptions();
        services.AddSingleton(jobOptions);

        services.AddSingleton<IPaymentQueue, ChannelPaymentQueue>();
        services.AddHostedService<PaymentProcessingWorker>();
        services.AddHostedService<FailedPaymentRetryWorker>();

        return services;
    }
}