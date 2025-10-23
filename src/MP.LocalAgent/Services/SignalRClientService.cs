using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MP.LocalAgent.Configuration;
using MP.LocalAgent.Contracts.Responses;
using MP.LocalAgent.Exceptions;
using MP.LocalAgent.Interfaces;
using Newtonsoft.Json;

namespace MP.LocalAgent.Services
{
    /// <summary>
    /// SignalR client service for communication with Azure API
    /// </summary>
    public class SignalRClientService : ISignalRClientService
    {
        private readonly ILogger<SignalRClientService> _logger;
        private readonly LocalAgentConfiguration _config;
        private HubConnection? _hubConnection;
        private ConnectionInfo? _connectionInfo;
        private readonly SemaphoreSlim _connectionLock = new(1, 1);
        private int _reconnectAttempts = 0;

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
        public Enums.AgentConnectionStatus ConnectionStatus { get; private set; } = Enums.AgentConnectionStatus.Disconnected;
        public ConnectionInfo? ConnectionInfo => _connectionInfo;

        public event EventHandler<string>? OnCommandReceived;
        public event EventHandler? OnConnected;
        public event EventHandler? OnDisconnected;
        public event EventHandler<ConnectionStatusChangedEventArgs>? ConnectionStatusChanged;
        public event EventHandler<SignalRErrorEventArgs>? OnError;

        public SignalRClientService(
            ILogger<SignalRClientService> logger,
            IOptions<LocalAgentConfiguration> config)
        {
            _logger = logger;
            _config = config.Value;
        }

        public async Task ConnectAsync(string serverUrl, Guid tenantId, string agentId)
        {
            await _connectionLock.WaitAsync();
            try
            {
                if (IsConnected)
                {
                    _logger.LogWarning("Already connected to SignalR");
                    return;
                }

                await SetConnectionStatusAsync(Enums.AgentConnectionStatus.Connecting, "Initializing connection");

                var hubUrl = $"{serverUrl.TrimEnd('/')}/hubs/localAgent";
                _logger.LogInformation("Connecting to SignalR hub: {HubUrl}", hubUrl);

                // Create hub connection
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(hubUrl, options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult(GetAccessToken(tenantId, agentId));
                        options.Headers["Tenant-Id"] = tenantId.ToString();
                        options.Headers["Agent-Id"] = agentId;
                    })
                    .WithAutomaticReconnect(new RetryPolicy())
                    .WithJsonProtocol(options =>
                    {
                        options.PayloadSerializerSettings = new JsonSerializerSettings
                        {
                            TypeNameHandling = TypeNameHandling.None
                        };
                    })
                    .Build();

                // Setup event handlers
                SetupHubEventHandlers();

                // Start connection
                await _hubConnection.StartAsync();

                // Update connection info
                _connectionInfo = new ConnectionInfo
                {
                    ServerUrl = serverUrl,
                    TenantId = tenantId,
                    AgentId = agentId,
                    ConnectedAt = DateTime.UtcNow,
                    LastHeartbeat = DateTime.UtcNow,
                    ConnectionId = _hubConnection.ConnectionId,
                    ReconnectCount = _reconnectAttempts
                };

                await SetConnectionStatusAsync(Enums.AgentConnectionStatus.Connected, "Connection established");
                _reconnectAttempts = 0;

                _logger.LogInformation("Successfully connected to SignalR hub with connection ID: {ConnectionId}",
                    _hubConnection.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to SignalR hub");
                await SetConnectionStatusAsync(Enums.AgentConnectionStatus.ConnectionFailed, ex.Message, ex);
                throw new SignalRConnectionException("SignalR connection failed", ex) { ServerUrl = serverUrl, AttemptCount = _reconnectAttempts };
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        public async Task DisconnectAsync()
        {
            await _connectionLock.WaitAsync();
            try
            {
                if (_hubConnection != null)
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                    _hubConnection = null;
                }

                _connectionInfo = null;
                await SetConnectionStatusAsync(Enums.AgentConnectionStatus.Disconnected, "Disconnected by request");

                _logger.LogInformation("Disconnected from SignalR hub");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SignalR disconnect");
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        public async Task SendCommandResponseAsync(CommandResponseBase response)
        {
            if (!IsConnected)
            {
                throw new SignalRConnectionException("Not connected to SignalR hub");
            }

            try
            {
                await _hubConnection!.SendAsync("CommandResponse", response);
                _logger.LogDebug("Command response sent for command {CommandId}", response.CommandId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send command response for {CommandId}", response.CommandId);
                throw new SignalRConnectionException("Failed to send command response", ex);
            }
        }

        public async Task SendDeviceStatusAsync(string deviceId, Enums.DeviceStatus status, string? details = null)
        {
            if (!IsConnected)
            {
                _logger.LogWarning("Cannot send device status - not connected to SignalR");
                return;
            }

            try
            {
                await _hubConnection!.SendAsync("DeviceStatusReport", new
                {
                    DeviceId = deviceId,
                    Status = status,
                    Details = details,
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogDebug("Device status sent for device {DeviceId}: {Status}", deviceId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send device status for {DeviceId}", deviceId);
            }
        }

        public async Task SendHeartbeatAsync()
        {
            if (!IsConnected)
            {
                _logger.LogDebug("Cannot send heartbeat - not connected to SignalR");
                return;
            }

            try
            {
                await _hubConnection!.SendAsync("Heartbeat");

                if (_connectionInfo != null)
                {
                    _connectionInfo.LastHeartbeat = DateTime.UtcNow;
                }

                _logger.LogDebug("Heartbeat sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send heartbeat");
            }
        }

        private void SetupHubEventHandlers()
        {
            if (_hubConnection == null) return;

            _hubConnection.On<string>("ExecuteCommand", async (commandJson) =>
            {
                _logger.LogInformation("Received command: {CommandJson}", commandJson);
                OnCommandReceived?.Invoke(this, commandJson);
            });

            _hubConnection.Reconnected += async (connectionId) =>
            {
                _logger.LogInformation("SignalR reconnected with connection ID: {ConnectionId}", connectionId);

                if (_connectionInfo != null)
                {
                    _connectionInfo.ConnectionId = connectionId;
                    _connectionInfo.ReconnectCount++;
                }

                await SetConnectionStatusAsync(Enums.AgentConnectionStatus.Connected, "Reconnected");
                OnConnected?.Invoke(this, EventArgs.Empty);
            };

            _hubConnection.Reconnecting += (exception) =>
            {
                _logger.LogWarning("SignalR reconnecting: {Error}", exception?.Message);
                _reconnectAttempts++;
                return Task.CompletedTask;
            };

            _hubConnection.Closed += async (exception) =>
            {
                _logger.LogWarning("SignalR connection closed: {Error}", exception?.Message);

                await SetConnectionStatusAsync(Enums.AgentConnectionStatus.Disconnected,
                    exception?.Message ?? "Connection closed", exception);
                OnDisconnected?.Invoke(this, EventArgs.Empty);
            };
        }

        private async Task SetConnectionStatusAsync(Enums.AgentConnectionStatus newStatus, string? message = null, Exception? exception = null)
        {
            var previousStatus = ConnectionStatus;
            ConnectionStatus = newStatus;

            _logger.LogInformation("SignalR connection status changed from {PreviousStatus} to {CurrentStatus}: {Message}",
                previousStatus, newStatus, message ?? "No message");

            ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs
            {
                PreviousStatus = previousStatus,
                CurrentStatus = newStatus,
                Message = message,
                Exception = exception
            });

            if (exception != null)
            {
                OnError?.Invoke(this, new SignalRErrorEventArgs
                {
                    Error = exception.Message,
                    Exception = exception,
                    IsRecoverable = newStatus == Enums.AgentConnectionStatus.Reconnecting
                });
            }
        }

        private string GetAccessToken(Guid tenantId, string agentId)
        {
            // TODO: Implement proper token generation
            // For now, return a simple token
            return $"{tenantId:N}:{agentId}:{DateTime.UtcNow:yyyyMMddHHmm}";
        }

        private class RetryPolicy : IRetryPolicy
        {
            public TimeSpan? NextRetryDelay(RetryContext retryContext)
            {
                var retryCount = retryContext.PreviousRetryCount;

                // Exponential backoff with jitter
                var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                var jitter = TimeSpan.FromMilliseconds(new Random().Next(0, 1000));
                var totalDelay = baseDelay + jitter;

                // Maximum delay of 30 seconds
                return totalDelay > TimeSpan.FromSeconds(30) ? TimeSpan.FromSeconds(30) : totalDelay;
            }
        }

        public void Dispose()
        {
            _ = Task.Run(DisconnectAsync);
            _connectionLock?.Dispose();
        }
    }
}