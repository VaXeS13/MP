using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using MP.LocalAgent.Services;
using MP.LocalAgent.BackgroundServices;
using MP.LocalAgent.DeviceServices;

namespace MP.LocalAgent
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            try
            {
                Log.Information("Starting MP Local Agent...");

                // Create host builder
                var hostBuilder = CreateHostBuilder(args, configuration);

                // Build and run host
                var host = hostBuilder.Build();

                // Initialize services
                await InitializeServicesAsync(host.Services, configuration);

                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService() // Enables running as Windows Service
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    // Configuration
                    services.AddSingleton(configuration);

                    // Core services
                    services.AddSingleton<IAgentService, AgentService>();
                    services.AddSingleton<IDeviceManager, DeviceManager>();
                    services.AddSingleton<ICommandQueue, CommandQueue>();
                    services.AddSingleton<ISignalRClientService, SignalRClientService>();

                    // Device services
                    services.AddSingleton<ITerminalService, TerminalService>();
                    services.AddSingleton<IFiscalPrinterService, FiscalPrinterService>();

                    // Background services
                    services.AddHostedService<AgentBackgroundService>();
                    services.AddHostedService<CommandProcessorService>();
                    services.AddHostedService<HeartbeatService>();

                    // Configuration binding
                    services.Configure<LocalAgentConfiguration>(configuration.GetSection("LocalAgent"));
                    services.Configure<DevicesConfiguration>(configuration.GetSection("Devices"));
                });

        private static async Task InitializeServicesAsync(IServiceProvider services, IConfiguration configuration)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            var agentService = services.GetRequiredService<IAgentService>();
            var deviceManager = services.GetRequiredService<IDeviceManager>();

            try
            {
                logger.LogInformation("Initializing MP Local Agent services...");

                // Read tenant and agent configuration
                var tenantId = configuration["LocalAgent:TenantId"];
                var agentId = configuration["LocalAgent:AgentId"];

                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(agentId))
                {
                    // Auto-generate if not provided
                    agentId = agentId ?? GenerateAgentId();
                    logger.LogWarning("TenantId or AgentId not configured. Generated AgentId: {AgentId}", agentId);
                }

                // Initialize agent service
                await agentService.InitializeAsync(Guid.Parse(tenantId), agentId);

                // Initialize device manager
                await deviceManager.InitializeAsync();

                logger.LogInformation("MP Local Agent services initialized successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize MP Local Agent services");
                throw;
            }
        }

        private static string GenerateAgentId()
        {
            var machineName = Environment.MachineName;
            var userDomainName = Environment.UserDomainName;
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            return $"{machineName}-{userDomainName}-{timestamp}";
        }
    }
}