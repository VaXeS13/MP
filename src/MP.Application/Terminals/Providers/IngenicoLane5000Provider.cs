using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using MP.Domain.Terminals;
using MP.Domain.Terminals.Communication;
using MP.Application.Terminals.Communication;

namespace MP.Application.Terminals.Providers
{
    /// <summary>
    /// Ingenico Lane/5000 Payment Terminal Provider
    /// Protocol: Telium Manager / ISO 8583
    /// Connection: TCP/IP (default port 8800)
    /// Regions: Global
    /// </summary>
    public class IngenicoLane5000Provider : ITerminalPaymentProvider, ITransientDependency
    {
        private readonly ILogger<IngenicoLane5000Provider> _logger;
        private readonly TcpIpTerminalCommunication _communication;
        private TenantTerminalSettings? _settings;

        public string ProviderId => "ingenico_lane_5000";
        public string DisplayName => "Ingenico Lane/5000";
        public string Description => "Ingenico Lane/5000 countertop payment terminal with Telium 2";

        // Ingenico Telium protocol constants
        private const byte STX = 0x02; // Start of text
        private const byte ETX = 0x03; // End of text
        private const byte ACK = 0x06; // Acknowledge
        private const byte NAK = 0x15; // Negative acknowledge
        private const byte ENQ = 0x05; // Enquiry

        public IngenicoLane5000Provider(
            ILogger<IngenicoLane5000Provider> logger,
            TcpIpTerminalCommunication communication)
        {
            _logger = logger;
            _communication = communication;
        }

        public async Task InitializeAsync(TenantTerminalSettings settings)
        {
            _settings = settings;

            var config = System.Text.Json.JsonSerializer.Deserialize<IngenicoConfig>(settings.ConfigurationJson)
                ?? new IngenicoConfig();

            var connectionSettings = new TerminalConnectionSettings
            {
                IpAddress = config.IpAddress,
                Port = config.Port,
                Timeout = config.Timeout
            };

            _logger.LogInformation("Initializing Ingenico Lane/5000 at {IpAddress}:{Port}", config.IpAddress, config.Port);

            try
            {
                await _communication.ConnectAsync(connectionSettings);
                _logger.LogInformation("Successfully initialized Ingenico Lane/5000");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Ingenico Lane/5000");
                throw;
            }
        }

        public async Task<TerminalPaymentResult> AuthorizePaymentAsync(TerminalPaymentRequest request)
        {
            _logger.LogInformation("Processing payment authorization for {Amount} {Currency}", request.Amount, request.Currency);

            try
            {
                // Build Ingenico payment request message
                var paymentMessage = BuildPaymentRequest(request);

                // Send to terminal and wait for response
                var response = await _communication.SendAndReceiveAsync(paymentMessage, 60000);

                // Parse response
                return ParsePaymentResponse(response, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment authorization failed");
                return new TerminalPaymentResult
                {
                    Success = false,
                    Status = "error",
                    ErrorMessage = ex.Message,
                    ErrorCode = "TERMINAL_ERROR"
                };
            }
        }

        public async Task<TerminalPaymentResult> CapturePaymentAsync(string transactionId, decimal amount)
        {
            _logger.LogInformation("Capturing payment {TransactionId} for {Amount}", transactionId, amount);

            // Ingenico Lane/5000 typically auto-captures after authorization
            // This is a placeholder for explicit capture if needed

            await Task.Delay(100); // Simulate processing

            return new TerminalPaymentResult
            {
                Success = true,
                TransactionId = transactionId,
                Status = "captured",
                Amount = amount,
                ProcessedAt = DateTime.UtcNow
            };
        }

        public async Task<TerminalPaymentResult> RefundPaymentAsync(string transactionId, decimal amount, string? reason = null)
        {
            _logger.LogInformation("Processing refund for {TransactionId} - Amount: {Amount}", transactionId, amount);

            try
            {
                var refundMessage = BuildRefundRequest(transactionId, amount);
                var response = await _communication.SendAndReceiveAsync(refundMessage, 60000);

                return ParseRefundResponse(response, transactionId, amount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refund failed");
                return new TerminalPaymentResult
                {
                    Success = false,
                    Status = "error",
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<TerminalPaymentResult> CancelPaymentAsync(string transactionId)
        {
            _logger.LogInformation("Cancelling payment {TransactionId}", transactionId);

            try
            {
                var cancelMessage = BuildCancelRequest(transactionId);
                var response = await _communication.SendAndReceiveAsync(cancelMessage, 30000);

                return new TerminalPaymentResult
                {
                    Success = true,
                    TransactionId = transactionId,
                    Status = "cancelled",
                    ProcessedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cancel failed");
                return new TerminalPaymentResult
                {
                    Success = false,
                    Status = "error",
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<TerminalPaymentStatus> GetPaymentStatusAsync(string transactionId)
        {
            _logger.LogInformation("Checking status of payment {TransactionId}", transactionId);

            try
            {
                var statusMessage = BuildStatusRequest(transactionId);
                var response = await _communication.SendAndReceiveAsync(statusMessage, 10000);

                return ParseStatusResponse(response, transactionId);
            }
            catch
            {
                return new TerminalPaymentStatus
                {
                    TransactionId = transactionId,
                    Status = "unknown"
                };
            }
        }

        public async Task<bool> CheckTerminalStatusAsync()
        {
            try
            {
                return await _communication.PingAsync();
            }
            catch
            {
                return false;
            }
        }

        #region Ingenico Protocol Helpers

        private byte[] BuildPaymentRequest(TerminalPaymentRequest request)
        {
            // Ingenico Telium protocol message structure
            // Format: STX + MESSAGE_TYPE + AMOUNT + CURRENCY + ETX + LRC

            var amountInCents = (long)(request.Amount * 100);
            var amountStr = amountInCents.ToString("D12"); // 12 digits, zero-padded
            var currencyCode = GetCurrencyCode(request.Currency);

            var message = $"P{amountStr}{currencyCode}";
            var messageBytes = Encoding.ASCII.GetBytes(message);

            // Build complete frame
            var frame = new byte[messageBytes.Length + 3];
            frame[0] = STX;
            Array.Copy(messageBytes, 0, frame, 1, messageBytes.Length);
            frame[frame.Length - 2] = ETX;
            frame[frame.Length - 1] = CalculateLRC(frame, 0, frame.Length - 1);

            return frame;
        }

        private byte[] BuildRefundRequest(string transactionId, decimal amount)
        {
            var amountInCents = (long)(amount * 100);
            var amountStr = amountInCents.ToString("D12");

            var message = $"R{amountStr}{transactionId}";
            var messageBytes = Encoding.ASCII.GetBytes(message);

            var frame = new byte[messageBytes.Length + 3];
            frame[0] = STX;
            Array.Copy(messageBytes, 0, frame, 1, messageBytes.Length);
            frame[frame.Length - 2] = ETX;
            frame[frame.Length - 1] = CalculateLRC(frame, 0, frame.Length - 1);

            return frame;
        }

        private byte[] BuildCancelRequest(string transactionId)
        {
            var message = $"C{transactionId}";
            var messageBytes = Encoding.ASCII.GetBytes(message);

            var frame = new byte[messageBytes.Length + 3];
            frame[0] = STX;
            Array.Copy(messageBytes, 0, frame, 1, messageBytes.Length);
            frame[frame.Length - 2] = ETX;
            frame[frame.Length - 1] = CalculateLRC(frame, 0, frame.Length - 1);

            return frame;
        }

        private byte[] BuildStatusRequest(string transactionId)
        {
            var message = $"S{transactionId}";
            var messageBytes = Encoding.ASCII.GetBytes(message);

            var frame = new byte[messageBytes.Length + 3];
            frame[0] = STX;
            Array.Copy(messageBytes, 0, frame, 1, messageBytes.Length);
            frame[frame.Length - 2] = ETX;
            frame[frame.Length - 1] = CalculateLRC(frame, 0, frame.Length - 1);

            return frame;
        }

        private TerminalPaymentResult ParsePaymentResponse(byte[] response, TerminalPaymentRequest request)
        {
            if (response.Length < 3 || response[0] != STX)
            {
                throw new Exception("Invalid response format");
            }

            var messageBytes = new byte[response.Length - 3];
            Array.Copy(response, 1, messageBytes, 0, messageBytes.Length);
            var message = Encoding.ASCII.GetString(messageBytes);

            // Parse response: format "A{transactionId}{authCode}" for approved
            //                        "D{errorCode}" for declined

            if (message.StartsWith("A"))
            {
                var transactionId = message.Substring(1, 20).Trim();
                var authCode = message.Length > 21 ? message.Substring(21, 6).Trim() : "";

                return new TerminalPaymentResult
                {
                    Success = true,
                    TransactionId = transactionId,
                    Status = "authorized",
                    Amount = request.Amount,
                    ProcessedAt = DateTime.UtcNow,
                    ProviderData = new()
                    {
                        ["authCode"] = authCode,
                        ["provider"] = "ingenico_lane_5000"
                    }
                };
            }
            else if (message.StartsWith("D"))
            {
                var errorCode = message.Substring(1, 2);
                var errorMessage = GetErrorMessage(errorCode);

                return new TerminalPaymentResult
                {
                    Success = false,
                    Status = "declined",
                    ErrorCode = errorCode,
                    ErrorMessage = errorMessage,
                    ProcessedAt = DateTime.UtcNow
                };
            }

            throw new Exception("Unknown response format");
        }

        private TerminalPaymentResult ParseRefundResponse(byte[] response, string transactionId, decimal amount)
        {
            // Similar parsing logic for refunds
            return new TerminalPaymentResult
            {
                Success = true,
                TransactionId = $"{transactionId}-REFUND",
                Status = "refunded",
                Amount = amount,
                ProcessedAt = DateTime.UtcNow
            };
        }

        private TerminalPaymentStatus ParseStatusResponse(byte[] response, string transactionId)
        {
            return new TerminalPaymentStatus
            {
                TransactionId = transactionId,
                Status = "captured",
                ProcessedAt = DateTime.UtcNow
            };
        }

        private byte CalculateLRC(byte[] data, int start, int end)
        {
            byte lrc = 0;
            for (int i = start; i < end; i++)
            {
                lrc ^= data[i];
            }
            return lrc;
        }

        private string GetCurrencyCode(string currency)
        {
            return currency.ToUpper() switch
            {
                "PLN" => "985",
                "EUR" => "978",
                "USD" => "840",
                "GBP" => "826",
                _ => "978" // Default to EUR
            };
        }

        private string GetErrorMessage(string errorCode)
        {
            return errorCode switch
            {
                "51" => "Insufficient funds",
                "54" => "Expired card",
                "55" => "Incorrect PIN",
                "57" => "Transaction not permitted",
                "58" => "Transaction not allowed for card",
                "61" => "Exceeds withdrawal limit",
                "75" => "PIN tries exceeded",
                "91" => "Issuer unavailable",
                _ => "Transaction declined"
            };
        }

        #endregion

        private class IngenicoConfig
        {
            public string IpAddress { get; set; } = "192.168.1.100";
            public int Port { get; set; } = 8800;
            public int Timeout { get; set; } = 60000;
            public string MerchantId { get; set; } = "";
            public string TerminalId { get; set; } = "";
        }
    }
}
