using Microsoft.Extensions.DependencyInjection;
using Payments.Application.Payments;
using Payments.Application.Providers;
using Payments.Application.Reports;

namespace Payments.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreatePaymentHandler>();
        services.AddScoped<GetPaymentHandler>();
        services.AddScoped<ListPaymentsHandler>();
        services.AddScoped<RetryPaymentHandler>();
        services.AddScoped<PaymentProcessor>();

        services.AddScoped<ListProvidersHandler>();
        services.AddScoped<CreateProviderHandler>();
        services.AddScoped<UpdateProviderHandler>();

        services.AddScoped<GetDailyReportHandler>();

        return services;
    }
}