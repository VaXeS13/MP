using System;
using System.Threading;
using System.Threading.Tasks;

namespace MP.Domain.Terminals.Communication
{
    /// <summary>
    /// Abstraction for terminal communication protocols
    /// Supports TCP/IP, USB, Serial Port, Bluetooth, REST API
    /// </summary>
    public interface ITerminalCommunication : IDisposable
    {
        /// <summary>
        /// Connection type identifier
        /// </summary>
        string ConnectionType { get; }

        /// <summary>
        /// Is connection currently open and ready
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Connect to the terminal device
        /// </summary>
        Task ConnectAsync(TerminalConnectionSettings settings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Disconnect from the terminal device
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Send data to terminal and wait for response
        /// </summary>
        Task<byte[]> SendAndReceiveAsync(byte[] data, int timeoutMs = 30000, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send data without waiting for response
        /// </summary>
        Task SendAsync(byte[] data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Read data from terminal
        /// </summary>
        Task<byte[]> ReceiveAsync(int timeoutMs = 30000, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if terminal responds to ping/status check
        /// </summary>
        Task<bool> PingAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Connection settings for terminal communication
    /// </summary>
    public class TerminalConnectionSettings
    {
        // TCP/IP Settings
        public string? IpAddress { get; set; }
        public int? Port { get; set; }

        // Serial Port Settings
        public string? PortName { get; set; }
        public int BaudRate { get; set; } = 9600;
        public string Parity { get; set; } = "None"; // None, Odd, Even, Mark, Space
        public int DataBits { get; set; } = 8;
        public int StopBits { get; set; } = 1; // 1, 1.5, 2

        // USB Settings
        public int? VendorId { get; set; }
        public int? ProductId { get; set; }
        public string? DevicePath { get; set; }

        // Bluetooth Settings
        public string? BluetoothAddress { get; set; }
        public string? BluetoothPin { get; set; }

        // REST API Settings
        public string? ApiBaseUrl { get; set; }
        public string? ApiKey { get; set; }
        public string? ApiSecret { get; set; }
        public string? AccessToken { get; set; }

        // Common Settings
        public int Timeout { get; set; } = 30000; // milliseconds
        public int MaxRetries { get; set; } = 3;
        public bool EnableLogging { get; set; } = true;
        public string? LogLevel { get; set; } = "Information";
    }

    /// <summary>
    /// Terminal communication exception
    /// </summary>
    public class TerminalCommunicationException : Exception
    {
        public string? ErrorCode { get; set; }
        public byte[]? SentData { get; set; }
        public byte[]? ReceivedData { get; set; }

        public TerminalCommunicationException(string message) : base(message) { }

        public TerminalCommunicationException(string message, Exception innerException)
            : base(message, innerException) { }

        public TerminalCommunicationException(string message, string errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public TerminalCommunicationException(string message, string errorCode, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
