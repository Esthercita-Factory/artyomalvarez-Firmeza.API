using System.Reflection;
using FluentValidation;
using Firmeza.Application.Interfaces.Services;
using Firmeza.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Firmeza.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // AutoMapper
        services.AddAutoMapper(assembly);

        // FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // Application Services
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<IRentalService, RentalService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
