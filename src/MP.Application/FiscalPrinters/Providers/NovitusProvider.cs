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
    /// Novitus Fiscal Printer Provider
    /// Popular models: Novitus Soleo, Nano E, Deon E, Bono E, Lupo E
    /// Used in: Poland
    /// Protocol: Novitus Protocol (ASCII-based with checksums)
    /// Connection: Serial Port (RS-232), USB (CDC), Ethernet (some models)
    /// Documentation: https://www.novitus.pl/
    /// </summary>
    public class NovitusProvider : IFiscalPrinterProvider, ITransientDependency
    {
        private readonly ILogger<NovitusProvider> _logger;
        private readonly SerialPortCommunication _communication;
        private FiscalPrinterSettings? _settings;

        // Novitus protocol control characters
        private const byte STX = 0x02;
        private const byte ETX = 0x03;
        private const byte ENQ = 0x05;
        private const byte ACK = 0x06;
        private const byte NAK = 0x15;
        private const byte DLE = 0x10;

        public string ProviderId => "novitus";
        public string DisplayName => "Novitus Fiscal Printer";
        public string[] SupportedRegions => new[] { "PL" };

        public NovitusProvider(
            ILogger<NovitusProvider> logger,
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
                "Initializing Novitus fiscal printer on port {PortName}",
                portName);

            // Connect via serial port
            var connectionSettings = new TerminalConnectionSettings
            {
                PortName = portName,
                BaudRate = baudRate, // Default 9600 for Novitus
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
                _logger.LogInformation("Novitus fiscal printer connected successfully");
            }
            else
            {
                _logger.LogWarning("Novitus fiscal printer status check returned unexpected response");
            }
        }

        public async Task<FiscalPrinterStatus> GetStatusAsync()
        {
            try
            {
                // Novitus status command: "status"
                var statusCommand = BuildCommand("status");
                var response = await _communication.SendAndReceiveAsync(statusCommand, 3000);

                if (response.Length < 3)
                {
                    return new FiscalPrinterStatus
                    {
                        IsOnline = false,
                        HasPaper = false,
                        FiscalMemoryOk = false,
                        ErrorMessage = "No response from printer"
                    };
                }

                // Parse Novitus status response
                var statusData = ParseResponse(response);

                var status = new FiscalPrinterStatus
                {
                    IsOnline = true,
                    HasPaper = !statusData.Contains("PAPER_LOW") && !statusData.Contains("PAPER_OUT"),
                    FiscalMemoryOk = !statusData.Contains("MEMORY_WARNING"),
                    IsInFiscalMode = statusData.Contains("FISCAL_MODE")
                };

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Novitus fiscal printer status");
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
                _logger.LogInformation("Printing fiscal receipt on Novitus printer");

                var commands = BuildReceiptCommands(request);

                string? receiptNumber = null;

                foreach (var command in commands)
                {
                    var commandBytes = BuildCommand(command);
                    var response = await _communication.SendAndReceiveAsync(commandBytes, 5000);

                    var responseData = ParseResponse(response);

                    if (responseData.StartsWith("ERROR"))
                    {
                        throw new FiscalPrinterException(
                            $"Printer error: {responseData}",
                            "PRINTER_ERROR");
                    }

                    // Extract receipt number from response
                    if (command.StartsWith("receipt") && responseData.Contains("NUMBER:"))
                    {
                        var parts = responseData.Split(':');
                        if (parts.Length > 1)
                        {
                            receiptNumber = parts[1].Trim();
                        }
                    }
                }

                return new FiscalReceiptResult
                {
                    Success = true,
                    FiscalNumber = $"FV/{DateTime.Now:yyyyMMdd}/{receiptNumber ?? "000"}",
                    FiscalDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    FiscalTime = DateTime.Now.ToString("HH:mm:ss"),
                    TotalAmount = request.TotalAmount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing fiscal receipt on Novitus printer");
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
                // Novitus non-fiscal print command
                var nonFiscalCommand = "nonfiscal\n" + string.Join("\n", lines);
                var commandBytes = BuildCommand(nonFiscalCommand);

                var response = await _communication.SendAndReceiveAsync(commandBytes, 5000);
                var responseData = ParseResponse(response);

                return !responseData.StartsWith("ERROR");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing non-fiscal text on Novitus printer");
                return false;
            }
        }

        public async Task<FiscalReportResult> GetDailyReportAsync(DateTime date)
        {
            try
            {
                // Novitus daily report command: "report daily"
                var reportCommand = BuildCommand("report daily");

                var response = await _communication.SendAndReceiveAsync(reportCommand, 10000);
                var responseData = ParseResponse(response);

                if (!responseData.StartsWith("ERROR"))
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
                _logger.LogError(ex, "Error printing daily report on Novitus printer");
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
                // Novitus cancellation command: "cancel last"
                var cancelCommand = BuildCommand($"cancel last\nreason:{reason}");
                var response = await _communication.SendAndReceiveAsync(cancelCommand, 5000);

                var responseData = ParseResponse(response);

                if (!responseData.StartsWith("ERROR"))
                {
                    _logger.LogInformation("Last receipt cancelled successfully. Reason: {Reason}", reason);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling last receipt on Novitus printer");
                return false;
            }
        }

        #region Helper Methods

        private byte[] BuildCommand(string command)
        {
            // Novitus frame: DLE STX + length + command + DLE ETX + checksum
            var commandBytes = Encoding.GetEncoding(1250).GetBytes(command); // Polish code page
            var length = (byte)commandBytes.Length;

            var frame = new byte[commandBytes.Length + 6];
            frame[0] = DLE;
            frame[1] = STX;
            frame[2] = length;
            Array.Copy(commandBytes, 0, frame, 3, commandBytes.Length);
            frame[frame.Length - 3] = DLE;
            frame[frame.Length - 2] = ETX;
            frame[frame.Length - 1] = CalculateChecksum(commandBytes);

            return frame;
        }

        private byte CalculateChecksum(byte[] data)
        {
            // Simple XOR checksum
            byte checksum = 0;
            foreach (var b in data)
            {
                checksum ^= b;
            }
            return checksum;
        }

        private string ParseResponse(byte[] response)
        {
            if (response.Length < 4)
                return "ERROR:NO_RESPONSE";

            // Expected format: DLE STX + length + data + DLE ETX + checksum
            if (response[0] == DLE && response[1] == STX)
            {
                var length = response[2];
                if (response.Length >= length + 6)
                {
                    var data = new byte[length];
                    Array.Copy(response, 3, data, 0, length);
                    return Encoding.GetEncoding(1250).GetString(data);
                }
            }

            return "ERROR:INVALID_RESPONSE";
        }

        private string[] BuildReceiptCommands(FiscalReceiptRequest request)
        {
            var commands = new List<string>();

            // Start fiscal receipt
            commands.Add("receipt start");

            // Add cashier info if provided
            if (!string.IsNullOrEmpty(request.CashierName))
            {
                commands.Add($"cashier:{EscapeText(request.CashierName)}");
            }

            // Add items
            foreach (var item in request.Items)
            {
                var itemName = EscapeText(item.Name);
                var qty = item.Quantity.ToString("F3");
                var price = item.UnitPrice.ToString("F2");
                var taxRateLetter = item.TaxRate; // Already in letter format (A, B, C, D, E)

                // Novitus item format: item:{name}:{qty}:{price}:{taxRate}
                commands.Add($"item:{itemName}:{qty}:{price}:{taxRateLetter}");
            }

            // Add payment
            var paymentType = request.PaymentMethod == "Cash" ? "0" : "1";
            commands.Add($"payment:{paymentType}:{request.TotalAmount:F2}");

            // End receipt
            commands.Add("receipt end");

            return commands.ToArray();
        }

        private string EscapeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text
                .Replace(":", ";") // Colon is used as delimiter
                .Replace("\r", "")
                .Replace("\n", " ")
                .Trim();
        }

        #endregion

        public void Dispose()
        {
            _communication?.Dispose();
        }
    }
}
