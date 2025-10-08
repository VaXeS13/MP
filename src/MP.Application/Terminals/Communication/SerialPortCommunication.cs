using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using MP.Domain.Terminals.Communication;

namespace MP.Application.Terminals.Communication
{
    /// <summary>
    /// Serial Port (RS-232) communication for terminals and fiscal printers
    /// Used by: Fiscal printers (Posnet, Elzab, Novitus), Legacy terminals
    /// </summary>
    public class SerialPortCommunication : ITerminalCommunication, ITransientDependency
    {
        private readonly ILogger<SerialPortCommunication> _logger;
        private SerialPort? _serialPort;
        private TerminalConnectionSettings? _settings;

        public string ConnectionType => "serial";
        public bool IsConnected => _serialPort?.IsOpen ?? false;

        public SerialPortCommunication(ILogger<SerialPortCommunication> logger)
        {
            _logger = logger;
        }

        public Task ConnectAsync(TerminalConnectionSettings settings, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(settings.PortName))
            {
                throw new TerminalCommunicationException("Port name is required for serial connection", "MISSING_PORT");
            }

            _settings = settings;

            try
            {
                _logger.LogInformation(
                    "Connecting to serial port {PortName} at {BaudRate} baud...",
                    settings.PortName, settings.BaudRate);

                _serialPort = new SerialPort
                {
                    PortName = settings.PortName,
                    BaudRate = settings.BaudRate,
                    Parity = ParseParity(settings.Parity),
                    DataBits = settings.DataBits,
                    StopBits = ParseStopBits(settings.StopBits),
                    ReadTimeout = settings.Timeout,
                    WriteTimeout = settings.Timeout,
                    Handshake = Handshake.None,
                    DtrEnable = true,
                    RtsEnable = true
                };

                _serialPort.Open();

                _logger.LogInformation("Successfully connected to serial port {PortName}", settings.PortName);

                return Task.CompletedTask;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied to serial port {PortName}", settings.PortName);
                throw new TerminalCommunicationException(
                    $"Access denied to port {settings.PortName}. Port may be in use by another application.",
                    "ACCESS_DENIED",
                    ex);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "I/O error opening serial port {PortName}", settings.PortName);
                throw new TerminalCommunicationException(
                    $"Failed to open port {settings.PortName}: {ex.Message}",
                    "PORT_ERROR",
                    ex);
            }
        }

        public Task DisconnectAsync()
        {
            try
            {
                if (_serialPort?.IsOpen == true)
                {
                    _serialPort.Close();
                    _logger.LogInformation("Disconnected from serial port");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during serial port disconnect");
            }

            return Task.CompletedTask;
        }

        public async Task<byte[]> SendAndReceiveAsync(byte[] data, int timeoutMs = 30000, CancellationToken cancellationToken = default)
        {
            if (!IsConnected || _serialPort == null)
            {
                throw new TerminalCommunicationException("Not connected to serial port", "NOT_CONNECTED");
            }

            try
            {
                _logger.LogDebug("Sending {Length} bytes to serial port", data.Length);

                // Clear any existing data in buffers
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();

                // Send data
                _serialPort.Write(data, 0, data.Length);

                // Wait for response
                using var timeoutCts = new CancellationTokenSource(timeoutMs);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                var response = await Task.Run(() =>
                {
                    var buffer = new System.Collections.Generic.List<byte>();
                    var startTime = DateTime.UtcNow;

                    while (!linkedCts.Token.IsCancellationRequested)
                    {
                        if (_serialPort.BytesToRead > 0)
                        {
                            var b = (byte)_serialPort.ReadByte();
                            buffer.Add(b);

                            // Check for message terminator (depends on protocol)
                            // Common terminators: CR (0x0D), LF (0x0A), ETX (0x03)
                            if (b == 0x0D || b == 0x0A || b == 0x03)
                            {
                                // Wait a bit to see if more data arrives
                                Thread.Sleep(50);
                                if (_serialPort.BytesToRead == 0)
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            Thread.Sleep(10);
                        }

                        // Additional timeout check
                        if ((DateTime.UtcNow - startTime).TotalMilliseconds > timeoutMs)
                        {
                            break;
                        }
                    }

                    return buffer.ToArray();
                }, linkedCts.Token);

                _logger.LogDebug("Received {Length} bytes from serial port", response.Length);

                return response;
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Serial communication timeout after {Timeout}ms", timeoutMs);
                throw new TerminalCommunicationException($"Communication timeout after {timeoutMs}ms", "TIMEOUT");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Serial port operation error");
                throw new TerminalCommunicationException($"Port error: {ex.Message}", "PORT_ERROR", ex);
            }
        }

        public Task SendAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (!IsConnected || _serialPort == null)
            {
                throw new TerminalCommunicationException("Not connected to serial port", "NOT_CONNECTED");
            }

            try
            {
                _serialPort.Write(data, 0, data.Length);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw new TerminalCommunicationException($"Send error: {ex.Message}", "SEND_ERROR", ex);
            }
        }

        public async Task<byte[]> ReceiveAsync(int timeoutMs = 30000, CancellationToken cancellationToken = default)
        {
            if (!IsConnected || _serialPort == null)
            {
                throw new TerminalCommunicationException("Not connected to serial port", "NOT_CONNECTED");
            }

            try
            {
                using var timeoutCts = new CancellationTokenSource(timeoutMs);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                return await Task.Run(() =>
                {
                    var buffer = new System.Collections.Generic.List<byte>();

                    while (!linkedCts.Token.IsCancellationRequested && _serialPort.BytesToRead > 0)
                    {
                        buffer.Add((byte)_serialPort.ReadByte());
                    }

                    return buffer.ToArray();
                }, linkedCts.Token);
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
                // Send ENQ (Enquiry) - standard status check
                var pingData = new byte[] { 0x05 };
                var response = await SendAndReceiveAsync(pingData, 2000, cancellationToken);

                // Expected response: ACK (0x06)
                return response.Length > 0 && (response[0] == 0x06 || response[0] == 0x05);
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            DisconnectAsync().GetAwaiter().GetResult();
            _serialPort?.Dispose();
        }

        #region Helper Methods

        private Parity ParseParity(string parity)
        {
            return parity?.ToUpper() switch
            {
                "NONE" => Parity.None,
                "ODD" => Parity.Odd,
                "EVEN" => Parity.Even,
                "MARK" => Parity.Mark,
                "SPACE" => Parity.Space,
                _ => Parity.None
            };
        }

        private StopBits ParseStopBits(int stopBits)
        {
            return stopBits switch
            {
                1 => StopBits.One,
                2 => StopBits.Two,
                _ => StopBits.One
            };
        }

        #endregion
    }
}
