using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using MP.Domain.Terminals.Communication;

namespace MP.Application.Terminals.Communication
{
    /// <summary>
    /// TCP/IP communication for terminals
    /// Used by: Ingenico Lane series, Verifone VX series, PAX network terminals
    /// </summary>
    public class TcpIpTerminalCommunication : ITerminalCommunication, ITransientDependency
    {
        private readonly ILogger<TcpIpTerminalCommunication> _logger;
        private TcpClient? _client;
        private NetworkStream? _stream;
        private TerminalConnectionSettings? _settings;

        public string ConnectionType => "tcp_ip";
        public bool IsConnected => _client?.Connected ?? false;

        public TcpIpTerminalCommunication(ILogger<TcpIpTerminalCommunication> logger)
        {
            _logger = logger;
        }

        public async Task ConnectAsync(TerminalConnectionSettings settings, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(settings.IpAddress))
            {
                throw new TerminalCommunicationException("IP address is required for TCP/IP connection", "MISSING_IP");
            }

            if (!settings.Port.HasValue || settings.Port.Value <= 0)
            {
                throw new TerminalCommunicationException("Valid port is required for TCP/IP connection", "MISSING_PORT");
            }

            _settings = settings;

            try
            {
                _logger.LogInformation("Connecting to terminal at {IpAddress}:{Port}...", settings.IpAddress, settings.Port);

                _client = new TcpClient();
                _client.SendTimeout = settings.Timeout;
                _client.ReceiveTimeout = settings.Timeout;

                await _client.ConnectAsync(settings.IpAddress, settings.Port.Value);
                _stream = _client.GetStream();

                _logger.LogInformation("Successfully connected to terminal at {IpAddress}:{Port}", settings.IpAddress, settings.Port);
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Failed to connect to terminal at {IpAddress}:{Port}", settings.IpAddress, settings.Port);
                throw new TerminalCommunicationException(
                    $"Failed to connect to terminal: {ex.Message}",
                    "CONNECTION_FAILED",
                    ex);
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                _stream?.Close();
                _client?.Close();
                _logger.LogInformation("Disconnected from terminal");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during disconnect");
            }

            await Task.CompletedTask;
        }

        public async Task<byte[]> SendAndReceiveAsync(byte[] data, int timeoutMs = 30000, CancellationToken cancellationToken = default)
        {
            if (!IsConnected || _stream == null)
            {
                throw new TerminalCommunicationException("Not connected to terminal", "NOT_CONNECTED");
            }

            try
            {
                _logger.LogDebug("Sending {Length} bytes to terminal", data.Length);

                // Send data
                await _stream.WriteAsync(data, 0, data.Length, cancellationToken);
                await _stream.FlushAsync(cancellationToken);

                // Receive response
                using var timeoutCts = new CancellationTokenSource(timeoutMs);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                var buffer = new byte[4096];
                var totalReceived = 0;

                using var ms = new MemoryStream();

                while (true)
                {
                    var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, linkedCts.Token);

                    if (bytesRead == 0)
                    {
                        break; // Connection closed
                    }

                    ms.Write(buffer, 0, bytesRead);
                    totalReceived += bytesRead;

                    // Check if we have a complete message
                    // This depends on the terminal protocol - might need terminator check
                    if (!_stream.DataAvailable)
                    {
                        break;
                    }
                }

                var response = ms.ToArray();
                _logger.LogDebug("Received {Length} bytes from terminal", response.Length);

                return response;
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Terminal communication timeout after {Timeout}ms", timeoutMs);
                throw new TerminalCommunicationException($"Communication timeout after {timeoutMs}ms", "TIMEOUT");
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "I/O error during terminal communication");
                throw new TerminalCommunicationException($"I/O error: {ex.Message}", "IO_ERROR", ex);
            }
        }

        public async Task SendAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (!IsConnected || _stream == null)
            {
                throw new TerminalCommunicationException("Not connected to terminal", "NOT_CONNECTED");
            }

            try
            {
                await _stream.WriteAsync(data, 0, data.Length, cancellationToken);
                await _stream.FlushAsync(cancellationToken);
            }
            catch (IOException ex)
            {
                throw new TerminalCommunicationException($"Send error: {ex.Message}", "SEND_ERROR", ex);
            }
        }

        public async Task<byte[]> ReceiveAsync(int timeoutMs = 30000, CancellationToken cancellationToken = default)
        {
            if (!IsConnected || _stream == null)
            {
                throw new TerminalCommunicationException("Not connected to terminal", "NOT_CONNECTED");
            }

            try
            {
                using var timeoutCts = new CancellationTokenSource(timeoutMs);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                var buffer = new byte[4096];
                using var ms = new MemoryStream();

                while (true)
                {
                    var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, linkedCts.Token);

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    ms.Write(buffer, 0, bytesRead);

                    if (!_stream.DataAvailable)
                    {
                        break;
                    }
                }

                return ms.ToArray();
            }
            catch (OperationCanceledException)
            {
                throw new TerminalCommunicationException($"Receive timeout after {timeoutMs}ms", "TIMEOUT");
            }
        }

        public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                return false;
            }

            try
            {
                // Send a simple status check (protocol-specific)
                // For Ingenico: 0x05 (ENQ - Enquiry)
                var pingData = new byte[] { 0x05 };
                var response = await SendAndReceiveAsync(pingData, 5000, cancellationToken);

                // Expected response: 0x06 (ACK)
                return response.Length > 0 && response[0] == 0x06;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            DisconnectAsync().GetAwaiter().GetResult();
            _stream?.Dispose();
            _client?.Dispose();
        }
    }
}
