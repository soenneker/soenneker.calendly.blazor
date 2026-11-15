using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Calendly.Blazor.Abstract;
using Soenneker.Blazor.Utils.ResourceLoader.Registrars;
namespace Soenneker.Calendly.Blazor.Registrars;
/// <summary>Registers Calendly browser services.</summary>
public static class CalendlyRegistrar
{
    /// <summary>Adds Calendly and its resource loader with scoped lifetimes.</summary>
    public static IServiceCollection AddCalendlyAsScoped(this IServiceCollection services)
    {
        services.AddResourceLoaderAsScoped();
        services.TryAddScoped<ICalendly, Calendly>();
        return services;
    }
}
