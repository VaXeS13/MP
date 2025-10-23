using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MP.LocalAgent.Configuration;
using MP.LocalAgent.Contracts.Models;
using MP.LocalAgent.Contracts.Enums;
using MP.LocalAgent.Exceptions;
using MP.LocalAgent.Interfaces;

namespace MP.LocalAgent.Services
{
    /// <summary>
    /// Main service for managing the local agent
    /// </summary>
    public class AgentService : IAgentService
    {
        private readonly ILogger<AgentService> _logger;
        private readonly LocalAgentConfiguration _config;
        private readonly IDeviceManager _deviceManager;
        private readonly ICommandQueue _commandQueue;
        private readonly ISignalRClientService _signalRClient;
        private readonly ITerminalService _terminalService;
        private readonly IFiscalPrinterService _fiscalPrinterService;

        private AgentStatus _status = AgentStatus.Stopped;
        private Guid _tenantId;
        private string _agentId = null!;
        private readonly SemaphoreSlim _statusLock = new(1, 1);

        public AgentStatus GetStatus() => _status;

        public event EventHandler<AgentStatusChangedEventArgs>? StatusChanged;

        public AgentService(
            ILogger<AgentService> logger,
            IOptions<LocalAgentConfiguration> config,
            IDeviceManager deviceManager,
            ICommandQueue commandQueue,
            ISignalRClientService signalRClient,
            ITerminalService terminalService,
            IFiscalPrinterService fiscalPrinterService)
        {
            _logger = logger;
            _config = config.Value;
            _deviceManager = deviceManager;
            _commandQueue = commandQueue;
            _signalRClient = signalRClient;
            _terminalService = terminalService;
            _fiscalPrinterService = fiscalPrinterService;

            // Subscribe to SignalR events
            _signalRClient.OnConnected += OnSignalRConnected;
            _signalRClient.OnDisconnected += OnSignalRDisconnected;
            _signalRClient.ConnectionStatusChanged += OnSignalRConnectionStatusChanged;
            _signalRClient.OnError += OnSignalRError;
        }

        public async Task InitializeAsync(Guid tenantId, string agentId)
        {
            _logger.LogInformation("Initializing agent for tenant {TenantId} with agent ID {AgentId}", tenantId, agentId);

            if (_status != AgentStatus.Stopped)
            {
                throw new AgentStateException("Agent must be stopped before initialization")
                {
                    ExpectedState = AgentStatus.Stopped.ToString(),
                    ActualState = _status.ToString()
                };
            }

            try
            {
                await SetStatusAsync(AgentStatus.Starting);

                _tenantId = tenantId;
                _agentId = agentId;

                // Initialize device manager
                await _deviceManager.InitializeAsync();

                _logger.LogInformation("Agent initialized successfully for tenant {TenantId}", tenantId);
                await SetStatusAsync(AgentStatus.Stopped);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize agent for tenant {TenantId}", tenantId);
                await SetStatusAsync(AgentStatus.Error);
                throw new AgentInitializationException("Failed to initialize agent", ex);
            }
        }

        public async Task<bool> StartAsync()
        {
            _logger.LogInformation("Starting agent {AgentId} for tenant {TenantId}", _agentId, _tenantId);

            if (_status != AgentStatus.Stopped)
            {
                _logger.LogWarning("Agent is already running (status: {Status})", _status);
                return false;
            }

            try
            {
                await SetStatusAsync(AgentStatus.Starting);

                // Connect to SignalR
                await _signalRClient.ConnectAsync(_config.ServerUrl, _tenantId, _agentId);

                // Register with cloud API
                await RegisterAgentAsync();

                await SetStatusAsync(AgentStatus.Running);
                _logger.LogInformation("Agent started successfully");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start agent");
                await SetStatusAsync(AgentStatus.Error);
                return false;
            }
        }

        public async Task StopAsync()
        {
            _logger.LogInformation("Stopping agent {AgentId}", _agentId);

            if (_status == AgentStatus.Stopped)
            {
                _logger.LogWarning("Agent is already stopped");
                return;
            }

            try
            {
                await SetStatusAsync(AgentStatus.Stopping);

                // Disconnect from SignalR
                await _signalRClient.DisconnectAsync();

                await SetStatusAsync(AgentStatus.Stopped);
                _logger.LogInformation("Agent stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during agent shutdown");
                await SetStatusAsync(AgentStatus.Error);
                throw;
            }
        }

        public async Task<AgentDeviceInfo> GetDeviceInfoAsync()
        {
            var devices = await _deviceManager.GetAllDevicesAsync();

            return new AgentDeviceInfo
            {
                TenantId = _tenantId,
                AgentId = _agentId,
                ComputerName = Environment.MachineName,
                IpAddress = GetLocalIpAddress(),
                Version = "1.0.0",
                StartedAt = DateTime.UtcNow,
                LastHeartbeat = DateTime.UtcNow,
                ConnectionStatus = _signalRClient.ConnectionStatus,
                Devices = devices
            };
        }

        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                // Check agent status
                if (_status != AgentStatus.Running)
                    return false;

                // Check SignalR connection
                if (!_signalRClient.IsConnected)
                    return false;

                // Check devices
                var devices = await _deviceManager.GetAllDevicesAsync();
                var hasAvailableDevices = devices.Exists(d => d.IsEnabled && d.Status == DeviceStatus.Ready);

                return hasAvailableDevices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return false;
            }
        }

        public async Task RegisterAgentAsync()
        {
            try
            {
                var deviceInfo = await GetDeviceInfoAsync();
                var deviceInfoJson = System.Text.Json.JsonSerializer.Serialize(deviceInfo);

                _logger.LogInformation("Registering agent with device info: {DeviceInfo}", deviceInfoJson);

                // Register agent with SignalR Hub
                await _signalRClient.InvokeAsync("RegisterAgent", new { DeviceInfo = deviceInfo });

                _logger.LogInformation("Agent registration completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register agent");
                throw new SignalRConnectionException("Agent registration failed", ex);
            }
        }

        public async Task SendHeartbeatAsync()
        {
            try
            {
                if (_signalRClient.IsConnected)
                {
                    await _signalRClient.SendHeartbeatAsync();
                    _logger.LogDebug("Heartbeat sent successfully");
                }
                else
                {
                    _logger.LogWarning("Cannot send heartbeat - SignalR not connected");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send heartbeat");
            }
        }

        private async Task SetStatusAsync(AgentStatus newStatus, string? message = null)
        {
            await _statusLock.WaitAsync();
            try
            {
                var previousStatus = _status;
                _status = newStatus;

                _logger.LogInformation("Agent status changed from {PreviousStatus} to {NewStatus}: {Message}",
                    previousStatus, newStatus, message ?? "No message");

                StatusChanged?.Invoke(this, new AgentStatusChangedEventArgs
                {
                    PreviousStatus = previousStatus,
                    CurrentStatus = newStatus,
                    Message = message
                });
            }
            finally
            {
                _statusLock.Release();
            }
        }

        private string? GetLocalIpAddress()
        {
            try
            {
                using var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, 0);
                socket.Connect("8.8.8.8", 65530);
                var endPoint = socket.LocalEndPoint as System.Net.IPEndPoint;
                return endPoint?.Address.ToString();
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        #region SignalR Event Handlers

        private async void OnSignalRConnected(object? sender, EventArgs e)
        {
            _logger.LogInformation("SignalR connection established");
        }

        private async void OnSignalRDisconnected(object? sender, EventArgs e)
        {
            _logger.LogWarning("SignalR connection lost");

            if (_status == AgentStatus.Running)
            {
                // Mark as error to indicate connectivity issues
                await SetStatusAsync(AgentStatus.Error, "SignalR connection lost");
            }
        }

        private async void OnSignalRConnectionStatusChanged(object? sender, ConnectionStatusChangedEventArgs e)
        {
            _logger.LogInformation("SignalR connection status changed from {PreviousStatus} to {CurrentStatus}: {Message}",
                e.PreviousStatus, e.CurrentStatus, e.Message);

            if (e.CurrentStatus == AgentConnectionStatus.Connected && _status == AgentStatus.Error)
            {
                // Recover from error state
                await SetStatusAsync(AgentStatus.Running, "SignalR connection restored");
            }
        }

        private void OnSignalRError(object? sender, SignalRErrorEventArgs e)
        {
            _logger.LogError(e.Exception, "SignalR error: {Error}", e.Error);
        }

        #endregion

        public void Dispose()
        {
            _statusLock?.Dispose();
        }
    }
}