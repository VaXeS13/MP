using System;
using System.Threading;
using System.Threading.Tasks;
using MP.LocalAgent.Contracts.Responses;

namespace MP.LocalAgent.Interfaces
{
    /// <summary>
    /// Service for SignalR communication with Azure API
    /// </summary>
    public interface ISignalRClientService
    {
        /// <summary>
        /// Connect to the Azure SignalR hub
        /// </summary>
        Task ConnectAsync(string serverUrl, Guid tenantId, string agentId);

        /// <summary>
        /// Disconnect from the SignalR hub
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Send command response back to Azure
        /// </summary>
        Task SendCommandResponseAsync(CommandResponseBase response);

        /// <summary>
        /// Send device status to Azure
        /// </summary>
        Task SendDeviceStatusAsync(string deviceId, Enums.DeviceStatus status, string? details = null);

        /// <summary>
        /// Send heartbeat to Azure
        /// </summary>
        Task SendHeartbeatAsync();

        /// <summary>
        /// Check if connected to SignalR
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Get connection status
        /// </summary>
        Enums.AgentConnectionStatus ConnectionStatus { get; }

        /// <summary>
        /// Get connection information
        /// </summary>
        ConnectionInfo? ConnectionInfo { get; }

        /// <summary>
        /// Event fired when a command is received from Azure
        /// </summary>
        event EventHandler<string>? OnCommandReceived;

        /// <summary>
        /// Event fired when connection is established
        /// </summary>
        event EventHandler? OnConnected;

        /// <summary>
        /// Event fired when connection is lost
        /// </summary>
        event EventHandler? OnDisconnected;

        /// <summary>
        /// Event fired when connection status changes
        /// </summary>
        event EventHandler<ConnectionStatusChangedEventArgs>? ConnectionStatusChanged;

        /// <summary>
        /// Event fired when an error occurs
        /// </summary>
        event EventHandler<SignalRErrorEventArgs>? OnError;
    }

    /// <summary>
    /// Connection information
    /// </summary>
    public class ConnectionInfo
    {
        public string ServerUrl { get; set; } = null!;
        public Guid TenantId { get; set; }
        public string AgentId { get; set; } = null!;
        public DateTime ConnectedAt { get; set; }
        public DateTime LastHeartbeat { get; set; }
        public string? ConnectionId { get; set; }
        public int ReconnectCount { get; set; }
        public TimeSpan RoundTripTime { get; set; }
    }

    /// <summary>
    /// Connection status changed event arguments
    /// </summary>
    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        public Enums.AgentConnectionStatus PreviousStatus { get; set; }
        public Enums.AgentConnectionStatus CurrentStatus { get; set; }
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Exception? Exception { get; set; }
    }

    /// <summary>
    /// SignalR error event arguments
    /// </summary>
    public class SignalRErrorEventArgs : EventArgs
    {
        public string Error { get; set; } = null!;
        public Exception? Exception { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsRecoverable { get; set; }
    }
}