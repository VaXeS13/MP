using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using MP.Domain.FiscalPrinters;
using MP.Domain.Terminals.Communication;
using MP.Application.Terminals.Communication;

namespace MP.Application.FiscalPrinters.Providers
{
    /// <summary>
    /// Elzab Fiscal Printer Provider
    /// Popular models: Elzab Omega, Sigma, K10, Mini E, Alfa
    /// Used in: Poland
    /// Protocol: Elzab Protocol (similar to ESC/POS with fiscal extensions)
    /// Connection: Serial Port (RS-232) or USB (CDC/ACM)
    /// Documentation: https://www.elzab.com.pl/
    /// </summary>
    public class ElzabProvider : IFiscalPrinterProvider, ITransientDependency
    {
        private readonly ILogger<ElzabProvider> _logger;
        private readonly SerialPortCommunication _communication;
        private FiscalPrinterSettings? _settings;

        // Elzab protocol control characters
        private const byte STX = 0x02;
        private const byte ETX = 0x03;
        private const byte ENQ = 0x05;
        private const byte ACK = 0x06;
        private const byte NAK = 0x15;
        private const byte ESC = 0x1B;

        public string ProviderId => "elzab";
        public string DisplayName => "Elzab Fiscal Printer";
        public string[] SupportedRegions => new[] { "PL" };

        public ElzabProvider(
            ILogger<ElzabProvider> logger,
            SerialPortCommunication communication)
        {
            _logger = logger;
            _communication = communication;
        }

        public async Task InitializeAsync(FiscalPrinterSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            var portName = settings.ConnectionSettings.PortName ?? "COM1";
            var baudRate = settings.ConnectionSettings.BaudRate > 0 ? settings.ConnectionSettings.BaudRate : 9600;

            _logger.LogInformation(
                "Initializing Elzab fiscal printer on port {PortName}",
                portName);

            // Connect via serial port
            var connectionSettings = new TerminalConnectionSettings
            {
                PortName = portName,
                BaudRate = baudRate, // Default 9600 for Elzab
                DataBits = 8,
                StopBits = 1,
                Parity = "NONE",
                Timeout = 5000
            };

            await _communication.ConnectAsync(connectionSettings);

            // Send ENQ to check connection
            var statusCheck = await _communication.SendAndReceiveAsync(new byte[] { ENQ }, 2000);

            if (statusCheck.Length > 0 && statusCheck[0] == ACK)
            {
                _logger.LogInformation("Elzab fiscal printer connected successfully");
            }
            else
            {
                _logger.LogWarning("Elzab fiscal printer status check returned unexpected response");
            }
        }

        public async Task<FiscalPrinterStatus> GetStatusAsync()
        {
            try
            {
                // Elzab status command: ESC r
                var statusCommand = new byte[] { ESC, (byte)'r' };
                var response = await _communication.SendAndReceiveAsync(statusCommand, 3000);

                if (response.Length < 2)
                {
                    return new FiscalPrinterStatus
                    {
                        IsOnline = false,
                        HasPaper = false,
                        FiscalMemoryOk = false,
                        ErrorMessage = "No response from printer"
                    };
                }

                // Parse status bytes
                // Byte 0: General status
                // Byte 1: Fiscal status
                var generalStatus = response[0];
                var fiscalStatus = response.Length > 1 ? response[1] : (byte)0;

                var status = new FiscalPrinterStatus
                {
                    IsOnline = true,
                    HasPaper = (generalStatus & 0x08) == 0, // Bit 3 = 0 means has paper
                    FiscalMemoryOk = (fiscalStatus & 0x02) == 0, // Bit 1 = 0 means memory OK
                    IsInFiscalMode = (fiscalStatus & 0x01) != 0, // Bit 0 = fiscal mode
                    LastFiscalNumber = ExtractTransactionNumber(response).ToString()
                };

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Elzab fiscal printer status");
                return new FiscalPrinterStatus
                {
                    IsOnline = false,
                    HasPaper = false,
                    FiscalMemoryOk = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<FiscalReceiptResult> PrintReceiptAsync(FiscalReceiptRequest request)
        {
            try
            {
                _logger.LogInformation("Printing fiscal receipt on Elzab printer");

                var commands = BuildReceiptCommands(request);

                foreach (var command in commands)
                {
                    var commandBytes = Encoding.GetEncoding(1250).GetBytes(command); // Polish code page
                    var frame = BuildCommandFrame(commandBytes);

                    var response = await _communication.SendAndReceiveAsync(frame, 5000);

                    if (response.Length == 0 || response[0] != ACK)
                    {
                        throw new FiscalPrinterException(
                            $"Printer rejected command: {command}",
                            "COMMAND_REJECTED");
                    }
                }

                // Get receipt number from printer status
                var status = await GetStatusAsync();

                return new FiscalReceiptResult
                {
                    Success = true,
                    FiscalNumber = $"FV/{DateTime.Now:yyyyMMdd}/{status.LastFiscalNumber}",
                    FiscalDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    FiscalTime = DateTime.Now.ToString("HH:mm:ss"),
                    TotalAmount = request.TotalAmount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing fiscal receipt on Elzab printer");
                return new FiscalReceiptResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<bool> PrintNonFiscalAsync(string[] lines)
        {
            try
            {
                // Elzab non-fiscal mode: ESC n
                var nonFiscalStart = new byte[] { ESC, (byte)'n' };
                await _communication.SendAndReceiveAsync(nonFiscalStart, 2000);

                foreach (var line in lines)
                {
                    var lineBytes = Encoding.GetEncoding(1250).GetBytes(line);
                    var frame = BuildCommandFrame(lineBytes);
                    await _communication.SendAndReceiveAsync(frame, 3000);
                }

                // End non-fiscal mode: ESC ESC
                var nonFiscalEnd = new byte[] { ESC, ESC };
                await _communication.SendAndReceiveAsync(nonFiscalEnd, 2000);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing non-fiscal text on Elzab printer");
                return false;
            }
        }

        public async Task<FiscalReportResult> GetDailyReportAsync(DateTime date)
        {
            try
            {
                // Elzab daily report command: ESC z (Z-report) or ESC x (X-report)
                var reportCommand = new byte[] { ESC, (byte)'x' }; // X-report (non-resetting)

                var response = await _communication.SendAndReceiveAsync(reportCommand, 10000);

                if (response.Length > 0 && response[0] == ACK)
                {
                    return new FiscalReportResult
                    {
                        ReportDate = date,
                        ReportType = "Daily",
                        TotalSales = 0, // Would need to parse from printer response
                        TotalTax = 0,
                        ReceiptCount = 0
                    };
                }
                else
                {
                    return new FiscalReportResult
                    {
                        ReportDate = date,
                        ReportType = "Daily",
                        TotalSales = 0,
                        TotalTax = 0,
                        ReceiptCount = 0
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing daily report on Elzab printer");
                return new FiscalReportResult
                {
                    ReportDate = date,
                    ReportType = "Daily",
                    TotalSales = 0,
                    TotalTax = 0,
                    ReceiptCount = 0
                };
            }
        }

        public async Task<bool> CancelLastReceiptAsync(string reason)
        {
            try
            {
                // Elzab cancellation command: ESC c
                var cancelCommand = new byte[] { ESC, (byte)'c' };
                var response = await _communication.SendAndReceiveAsync(cancelCommand, 5000);

                if (response.Length > 0 && response[0] == ACK)
                {
                    _logger.LogInformation("Last receipt cancelled successfully. Reason: {Reason}", reason);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling last receipt on Elzab printer");
                return false;
            }
        }

        #region Helper Methods

        private byte[] BuildCommandFrame(byte[] data)
        {
            // Elzab frame: STX + data + ETX + BCC
            var frame = new byte[data.Length + 3];
            frame[0] = STX;
            Array.Copy(data, 0, frame, 1, data.Length);
            frame[frame.Length - 2] = ETX;
            frame[frame.Length - 1] = CalculateBCC(data);

            return frame;
        }

        private byte CalculateBCC(byte[] data)
        {
            // Block Check Character (XOR of all data bytes)
            byte bcc = 0;
            foreach (var b in data)
            {
                bcc ^= b;
            }
            return bcc;
        }

        private string[] BuildReceiptCommands(FiscalReceiptRequest request)
        {
            var commands = new List<string>();

            // Start fiscal receipt
            commands.Add("#h");

            // Add cashier info if provided
            if (!string.IsNullOrEmpty(request.CashierName))
            {
                commands.Add($"#k{EscapeText(request.CashierName)}");
            }

            // Add items
            foreach (var item in request.Items)
            {
                var itemName = EscapeText(item.Name);
                var qty = item.Quantity.ToString("F3");
                var price = item.UnitPrice.ToString("F2");
                var taxRateLetter = item.TaxRate; // Already in letter format (A, B, C, D, E)

                // Elzab item command: #l{name}\t{qty}\t{price}\t{taxRate}
                commands.Add($"#l{itemName}\t{qty}\t{price}\t{taxRateLetter}");
            }

            // Add payment
            if (request.PaymentMethod == "Cash")
            {
                commands.Add($"#p0\t{request.TotalAmount:F2}"); // Payment type 0 = Cash
            }
            else if (request.PaymentMethod == "Card")
            {
                commands.Add($"#p1\t{request.TotalAmount:F2}"); // Payment type 1 = Card
            }

            // End receipt
            commands.Add("#e");

            return commands.ToArray();
        }

        private string EscapeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text
                .Replace("\t", " ")
                .Replace("\r", "")
                .Replace("\n", " ")
                .Trim();
        }

        private int ExtractTransactionNumber(byte[] response)
        {
            // Try to extract transaction number from status response
            // This is printer-specific and may need adjustment
            if (response.Length >= 6)
            {
                try
                {
                    // Assuming last 4 bytes contain transaction counter
                    return BitConverter.ToInt32(response, response.Length - 4);
                }
                catch
                {
                    return 0;
                }
            }
            return 0;
        }

        #endregion

        public void Dispose()
        {
            _communication?.Dispose();
        }
    }
}
