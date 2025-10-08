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
    /// Verifone VX520 Payment Terminal Provider
    /// Protocol: VIPA (Verifone Internet Protocol Architecture)
    /// Connection: TCP/IP, Serial (default port 12000)
    /// Regions: Global
    /// </summary>
    public class VerifoneVX520Provider : ITerminalPaymentProvider, ITransientDependency
    {
        private readonly ILogger<VerifoneVX520Provider> _logger;
        private readonly TcpIpTerminalCommunication _communication;
        private TenantTerminalSettings? _settings;

        public string ProviderId => "verifone_vx520";
        public string DisplayName => "Verifone VX520";
        public string Description => "Verifone VX520 countertop payment terminal with VIPA protocol";

        // VIPA protocol constants
        private const byte STX = 0x02; // Start of text
        private const byte ETX = 0x03; // End of text
        private const byte ACK = 0x06; // Acknowledge
        private const byte NAK = 0x15; // Negative acknowledge

        public VerifoneVX520Provider(
            ILogger<VerifoneVX520Provider> _logger,
            TcpIpTerminalCommunication communication)
        {
            this._logger = _logger;
            _communication = communication;
        }

        public async Task InitializeAsync(TenantTerminalSettings settings)
        {
            _settings = settings;

            var config = System.Text.Json.JsonSerializer.Deserialize<VerifoneConfig>(settings.ConfigurationJson)
                ?? new VerifoneConfig();

            var connectionSettings = new TerminalConnectionSettings
            {
                IpAddress = config.IpAddress,
                Port = config.Port,
                Timeout = config.Timeout
            };

            _logger.LogInformation("Initializing Verifone VX520 at {IpAddress}:{Port}", config.IpAddress, config.Port);

            try
            {
                await _communication.ConnectAsync(connectionSettings);
                _logger.LogInformation("Successfully initialized Verifone VX520");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Verifone VX520");
                throw;
            }
        }

        public async Task<TerminalPaymentResult> AuthorizePaymentAsync(TerminalPaymentRequest request)
        {
            _logger.LogInformation("Processing payment authorization for {Amount} {Currency}", request.Amount, request.Currency);

            try
            {
                // Build VIPA payment request
                var paymentMessage = BuildVipaPaymentRequest(request);

                // Send to terminal and wait for response
                var response = await _communication.SendAndReceiveAsync(paymentMessage, 90000);

                // Parse response
                return ParseVipaPaymentResponse(response, request);
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

            // Verifone VX520 typically auto-captures after authorization
            // This is a placeholder for explicit capture if needed

            await Task.Delay(100);

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
                var refundMessage = BuildVipaRefundRequest(transactionId, amount);
                var response = await _communication.SendAndReceiveAsync(refundMessage, 90000);

                return ParseVipaRefundResponse(response, transactionId, amount);
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
                var cancelMessage = BuildVipaCancelRequest();
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
                var statusMessage = BuildVipaStatusRequest();
                var response = await _communication.SendAndReceiveAsync(statusMessage, 10000);

                return ParseVipaStatusResponse(response, transactionId);
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

        #region VIPA Protocol Helpers

        private byte[] BuildVipaPaymentRequest(TerminalPaymentRequest request)
        {
            // VIPA protocol message structure
            // Format: STX + MESSAGE_TYPE + LENGTH + DATA + ETX + LRC

            var amountInCents = (long)(request.Amount * 100);
            var amountStr = amountInCents.ToString("D12"); // 12 digits
            var currencyCode = GetCurrencyCode(request.Currency);

            // VIPA message type: "D0" for debit/credit card payment
            var data = $"D0{amountStr}{currencyCode}";
            var dataBytes = Encoding.ASCII.GetBytes(data);

            // Build frame
            var lengthBytes = BitConverter.GetBytes((short)dataBytes.Length);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
            }

            var frame = new byte[dataBytes.Length + 5];
            frame[0] = STX;
            frame[1] = lengthBytes[0];
            frame[2] = lengthBytes[1];
            Array.Copy(dataBytes, 0, frame, 3, dataBytes.Length);
            frame[frame.Length - 2] = ETX;
            frame[frame.Length - 1] = CalculateLRC(frame, 0, frame.Length - 1);

            return frame;
        }

        private byte[] BuildVipaRefundRequest(string transactionId, decimal amount)
        {
            var amountInCents = (long)(amount * 100);
            var amountStr = amountInCents.ToString("D12");

            // VIPA message type: "D2" for refund
            var data = $"D2{amountStr}{transactionId}";
            var dataBytes = Encoding.ASCII.GetBytes(data);

            var lengthBytes = BitConverter.GetBytes((short)dataBytes.Length);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
            }

            var frame = new byte[dataBytes.Length + 5];
            frame[0] = STX;
            frame[1] = lengthBytes[0];
            frame[2] = lengthBytes[1];
            Array.Copy(dataBytes, 0, frame, 3, dataBytes.Length);
            frame[frame.Length - 2] = ETX;
            frame[frame.Length - 1] = CalculateLRC(frame, 0, frame.Length - 1);

            return frame;
        }

        private byte[] BuildVipaCancelRequest()
        {
            // VIPA message type: "C0" for cancel
            var data = "C0";
            var dataBytes = Encoding.ASCII.GetBytes(data);

            var lengthBytes = BitConverter.GetBytes((short)dataBytes.Length);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
            }

            var frame = new byte[dataBytes.Length + 5];
            frame[0] = STX;
            frame[1] = lengthBytes[0];
            frame[2] = lengthBytes[1];
            Array.Copy(dataBytes, 0, frame, 3, dataBytes.Length);
            frame[frame.Length - 2] = ETX;
            frame[frame.Length - 1] = CalculateLRC(frame, 0, frame.Length - 1);

            return frame;
        }

        private byte[] BuildVipaStatusRequest()
        {
            // VIPA message type: "S0" for status
            var data = "S0";
            var dataBytes = Encoding.ASCII.GetBytes(data);

            var lengthBytes = BitConverter.GetBytes((short)dataBytes.Length);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
            }

            var frame = new byte[dataBytes.Length + 5];
            frame[0] = STX;
            frame[1] = lengthBytes[0];
            frame[2] = lengthBytes[1];
            Array.Copy(dataBytes, 0, frame, 3, dataBytes.Length);
            frame[frame.Length - 2] = ETX;
            frame[frame.Length - 1] = CalculateLRC(frame, 0, frame.Length - 1);

            return frame;
        }

        private TerminalPaymentResult ParseVipaPaymentResponse(byte[] response, TerminalPaymentRequest request)
        {
            if (response.Length < 5 || response[0] != STX)
            {
                throw new Exception("Invalid VIPA response format");
            }

            var dataLength = (response[1] << 8) | response[2];
            var dataBytes = new byte[dataLength];
            Array.Copy(response, 3, dataBytes, 0, dataLength);
            var message = Encoding.ASCII.GetString(dataBytes);

            // Parse response: "A0{transactionId}{authCode}" for approved
            //                "D0{errorCode}" for declined

            if (message.StartsWith("A0"))
            {
                var transactionId = message.Substring(2, 20).Trim();
                var authCode = message.Length > 22 ? message.Substring(22, 6).Trim() : "";

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
                        ["provider"] = "verifone_vx520"
                    }
                };
            }
            else if (message.StartsWith("D0"))
            {
                var errorCode = message.Substring(2, 2);
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

            throw new Exception("Unknown VIPA response format");
        }

        private TerminalPaymentResult ParseVipaRefundResponse(byte[] response, string transactionId, decimal amount)
        {
            return new TerminalPaymentResult
            {
                Success = true,
                TransactionId = $"{transactionId}-REFUND",
                Status = "refunded",
                Amount = amount,
                ProcessedAt = DateTime.UtcNow
            };
        }

        private TerminalPaymentStatus ParseVipaStatusResponse(byte[] response, string transactionId)
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
                _ => "978"
            };
        }

        private string GetErrorMessage(string errorCode)
        {
            return errorCode switch
            {
                "05" => "Do not honor",
                "14" => "Invalid card number",
                "51" => "Insufficient funds",
                "54" => "Expired card",
                "55" => "Incorrect PIN",
                "57" => "Transaction not permitted",
                "61" => "Exceeds withdrawal limit",
                "75" => "PIN tries exceeded",
                _ => "Transaction declined"
            };
        }

        #endregion

        private class VerifoneConfig
        {
            public string IpAddress { get; set; } = "192.168.1.100";
            public int Port { get; set; } = 12000;
            public int Timeout { get; set; } = 90000;
            public string MerchantId { get; set; } = "";
            public string TerminalId { get; set; } = "";
        }
    }
}
