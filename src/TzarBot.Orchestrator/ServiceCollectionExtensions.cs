using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TzarBot.Orchestrator.Communication;
using TzarBot.Orchestrator.Service;
using TzarBot.Orchestrator.VM;

namespace TzarBot.Orchestrator;

/// <summary>
/// Extension methods for registering Orchestrator services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the TzarBot Orchestrator services to the service collection
    /// </summary>
    public static IServiceCollection AddTzarBotOrchestrator(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration
        services.Configure<HyperVManagerOptions>(
            configuration.GetSection(HyperVManagerOptions.SectionName));

        services.Configure<VMPoolOptions>(
            configuration.GetSection(VMPoolOptions.SectionName));

        services.Configure<CommunicationOptions>(
            configuration.GetSection(CommunicationOptions.SectionName));

        services.Configure<OrchestratorConfig>(
            configuration.GetSection(OrchestratorConfig.SectionName));

        // VM Management
        services.AddSingleton<IVMManager, HyperVManager>();
        services.AddSingleton<VMPool>();

        // Communication
        services.AddSingleton<IVMCommunicator, PowerShellCommunicator>();
        services.AddSingleton<GenomeTransfer>();

        // Orchestrator Service
        services.AddHostedService<OrchestratorService>();
        services.AddSingleton<OrchestratorService>(sp =>
            (OrchestratorService)sp.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
                .First(s => s is OrchestratorService));

        return services;
    }
}
