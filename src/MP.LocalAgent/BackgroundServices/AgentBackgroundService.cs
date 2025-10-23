using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MP.LocalAgent.Interfaces;

namespace MP.LocalAgent.BackgroundServices
{
    /// <summary>
    /// Main background service for the local agent
    /// </summary>
    public class AgentBackgroundService : BackgroundService
    {
        private readonly ILogger<AgentBackgroundService> _logger;
        private readonly IAgentService _agentService;
        private readonly IDeviceManager _deviceManager;
        private readonly ISignalRClientService _signalRClient;

        public AgentBackgroundService(
            ILogger<AgentBackgroundService> logger,
            IAgentService agentService,
            IDeviceManager deviceManager,
            ISignalRClientService signalRClient)
        {
            _logger = logger;
            _agentService = agentService;
            _deviceManager = deviceManager;
            _signalRClient = signalRClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Agent background service starting");

            try
            {
                // Main agent lifecycle
                await RunAgentLifecycleAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Agent background service stopping due to cancellation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent background service encountered an error");
                throw;
            }

            _logger.LogInformation("Agent background service stopped");
        }

        private async Task RunAgentLifecycleAsync(CancellationToken stoppingToken)
        {
            // Start the agent
            var started = await _agentService.StartAsync();
            if (!started)
            {
                _logger.LogError("Failed to start agent service");
                return;
            }

            _logger.LogInformation("Agent started successfully");

            // Main monitoring loop
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Perform periodic health checks
                    await PerformHealthCheckAsync();

                    // Wait before next check
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in agent monitoring loop");
                    // Continue the loop despite errors
                }
            }

            // Cleanup
            _logger.LogInformation("Stopping agent service");
            await _agentService.StopAsync();
        }

        private async Task PerformHealthCheckAsync()
        {
            try
            {
                var isHealthy = await _agentService.IsHealthyAsync();
                if (!isHealthy)
                {
                    _logger.LogWarning("Agent health check failed");

                    // Check individual components
                    if (!_signalRClient.IsConnected)
                    {
                        _logger.LogWarning("SignalR client is disconnected");
                    }

                    var devices = await _deviceManager.GetAllDevicesAsync();
                    var availableDevices = devices.FindAll(d => d.IsEnabled && d.Status == Enums.DeviceStatus.Ready);
                    if (availableDevices.Count == 0)
                    {
                        _logger.LogWarning("No devices are available");
                    }
                }
                else
                {
                    _logger.LogDebug("Agent health check passed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during health check");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Agent background service stop requested");

            try
            {
                await base.StopAsync(cancellationToken);
                await _agentService.StopAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during agent background service shutdown");
            }
        }
    }
}