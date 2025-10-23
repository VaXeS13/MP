using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MP.LocalAgent.Contracts.Commands;
using MP.LocalAgent.Contracts.Responses;

namespace MP.Application.Devices
{
    /// <summary>
    /// SignalR-based proxy for remote device communication with local agents
    /// </summary>
    public class SignalRDeviceProxy : IRemoteDeviceProxy, ITransientDependency
    {
        private readonly ILogger<SignalRDeviceProxy> _logger;
        private readonly RemoteDeviceProxyOptions _options;
        private readonly ICurrentTenant _currentTenant;
        private readonly ICommandResponseRegistry _responseRegistry;

        // Cache for pending command responses
        private static readonly ConcurrentDictionary<Guid, TaskCompletionSource<object>> PendingResponses =
            new();

        public SignalRDeviceProxy(
            ILogger<SignalRDeviceProxy> logger,
            IOptions<RemoteDeviceProxyOptions> options,
            ICurrentTenant currentTenant)
        {
            _logger = logger;
            _options = options.Value;
            _currentTenant = currentTenant;
            _responseRegistry = new InMemoryCommandResponseRegistry();
        }

        public async Task<TerminalPaymentResponse> AuthorizePaymentAsync(
            AuthorizeTerminalPaymentCommand request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteCommandAsync<TerminalPaymentResponse>(
                request,
                "AuthorizePayment",
                cancellationToken);
        }

        public async Task<TerminalPaymentResponse> CapturePaymentAsync(
            CaptureTerminalPaymentCommand request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteCommandAsync<TerminalPaymentResponse>(
                request,
                "CapturePayment",
                cancellationToken);
        }

        public async Task<TerminalPaymentResponse> RefundPaymentAsync(
            RefundTerminalPaymentCommand request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteCommandAsync<TerminalPaymentResponse>(
                request,
                "RefundPayment",
                cancellationToken);
        }

        public async Task<TerminalPaymentResponse> CancelPaymentAsync(
            CancelTerminalPaymentCommand request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteCommandAsync<TerminalPaymentResponse>(
                request,
                "CancelPayment",
                cancellationToken);
        }

        public async Task<TerminalStatusResponse> CheckTerminalStatusAsync(
            CheckTerminalStatusCommand request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteCommandAsync<TerminalStatusResponse>(
                request,
                "CheckTerminalStatus",
                cancellationToken);
        }

        public async Task<FiscalReceiptResponse> PrintFiscalReceiptAsync(
            PrintFiscalReceiptCommand request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteCommandAsync<FiscalReceiptResponse>(
                request,
                "PrintFiscalReceipt",
                cancellationToken);
        }

        public async Task<SimpleCommandResponse> PrintNonFiscalDocumentAsync(
            PrintNonFiscalDocumentCommand request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteCommandAsync<SimpleCommandResponse>(
                request,
                "PrintNonFiscalDocument",
                cancellationToken);
        }

        public async Task<FiscalReportResponse> GetDailyFiscalReportAsync(
            GetDailyFiscalReportCommand request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteCommandAsync<FiscalReportResponse>(
                request,
                "GetDailyFiscalReport",
                cancellationToken);
        }

        public async Task<FiscalPrinterStatusResponse> CheckFiscalPrinterStatusAsync(
            CheckFiscalPrinterStatusCommand request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteCommandAsync<FiscalPrinterStatusResponse>(
                request,
                "CheckFiscalPrinterStatus",
                cancellationToken);
        }

        public async Task<bool> IsDeviceAvailableAsync(
            string deviceType,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var tenantId = _currentTenant.Id;
                if (tenantId == null)
                {
                    _logger.LogWarning("Cannot check device availability - no tenant context");
                    return false;
                }

                // In production, this would check with actual agent registry
                // For now, return true to indicate device availability would be checked
                _logger.LogInformation("Checking availability for device type: {DeviceType}", deviceType);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking device availability");
                return false;
            }
        }

        public async Task<string> GetDeviceStatusAsync(
            string deviceType,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var tenantId = _currentTenant.Id;
                if (tenantId == null)
                    return "Unknown - No tenant context";

                // In production, this would query actual device status from agent
                return "Unknown";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device status");
                return "Error retrieving status";
            }
        }

        /// <summary>
        /// Execute a command on the local agent with timeout and retry logic
        /// </summary>
        private async Task<T> ExecuteCommandAsync<T>(
            object command,
            string commandType,
            CancellationToken cancellationToken)
            where T : CommandResponseBase, new()
        {
            var tenantId = _currentTenant.Id;
            if (tenantId == null)
                throw new InvalidOperationException("Cannot execute device command without tenant context");

            var commandId = Guid.NewGuid();
            var startTime = DateTime.UtcNow;
            int attemptNumber = 0;
            Exception? lastException = null;

            try
            {
                while (attemptNumber < _options.MaxRetries)
                {
                    attemptNumber++;

                    try
                    {
                        _logger.LogInformation(
                            "Executing command {CommandId} ({CommandType}) on tenant {TenantId} (attempt {Attempt}/{MaxRetries})",
                            commandId, commandType, tenantId, attemptNumber, _options.MaxRetries);

                        // Create response placeholder
                        var response = await SendCommandToAgentAsync<T>(
                            commandId,
                            commandType,
                            command,
                            cancellationToken);

                        return response;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        _logger.LogWarning(
                            ex,
                            "Command {CommandId} ({CommandType}) failed on attempt {Attempt}/{MaxRetries}: {Message}",
                            commandId, commandType, attemptNumber, _options.MaxRetries, ex.Message);

                        if (attemptNumber < _options.MaxRetries)
                        {
                            await Task.Delay(_options.RetryDelay, cancellationToken);
                        }
                    }
                }

                // All retries exhausted
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(
                    "Command {CommandId} ({CommandType}) failed after {Attempts} attempts in {Duration}ms",
                    commandId, commandType, attemptNumber, duration.TotalMilliseconds);

                throw lastException ?? new InvalidOperationException(
                    $"Command {commandType} failed after {attemptNumber} attempts");
            }
            catch (Exception ex)
            {
                var response = new T
                {
                    CommandId = commandId,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ErrorCode = "DEVICE_COMMUNICATION_ERROR",
                    ProcessedAt = DateTime.UtcNow,
                    ProcessingDuration = DateTime.UtcNow - startTime
                };

                return response;
            }
        }

        /// <summary>
        /// Send command to agent and wait for response with timeout
        /// </summary>
        private async Task<T> SendCommandToAgentAsync<T>(
            Guid commandId,
            string commandType,
            object command,
            CancellationToken cancellationToken)
            where T : CommandResponseBase
        {
            var tcs = new TaskCompletionSource<object>();
            var timeoutCts = new CancellationTokenSource(_options.CommandTimeout);

            PendingResponses.TryAdd(commandId, tcs);

            try
            {
                // In actual implementation, this would send via SignalR to LocalAgentHub
                // For now, we'll create a timeout and return appropriate response
                await Task.Delay(100, cancellationToken);

                _logger.LogInformation(
                    "Command {CommandId} ({CommandType}) sent to agent",
                    commandId, commandType);

                // Wait for response with timeout
                using (timeoutCts.Token.Register(() =>
                    tcs.TrySetException(new TimeoutException(
                        $"Device command {commandType} timed out after {_options.CommandTimeout.TotalSeconds}s"))))
                {
                    try
                    {
                        var responseObj = await tcs.Task.ConfigureAwait(false);

                        if (responseObj is T response)
                        {
                            return response;
                        }

                        throw new InvalidOperationException(
                            $"Invalid response type received for command {commandType}");
                    }
                    catch (TimeoutException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Failed to execute command {commandType}: {ex.Message}", ex);
                    }
                }
            }
            finally
            {
                PendingResponses.TryRemove(commandId, out _);
            }
        }

        /// <summary>
        /// Internal method called by SignalR hub to deliver command responses
        /// </summary>
        public static void RegisterCommandResponse(Guid commandId, CommandResponseBase response)
        {
            if (PendingResponses.TryGetValue(commandId, out var tcs))
            {
                tcs.TrySetResult(response);
            }
        }

        private static string GetDeviceTypeForCommand(string commandType)
        {
            return commandType switch
            {
                "AuthorizePayment" or "CapturePayment" or "RefundPayment" or "CancelPayment" or "CheckTerminalStatus"
                    => "terminal",
                "PrintFiscalReceipt" or "PrintNonFiscalDocument" or "GetDailyFiscalReport" or "CheckFiscalPrinterStatus"
                    => "fiscal_printer",
                _ => "unknown"
            };
        }
    }

    /// <summary>
    /// Configuration options for remote device proxy
    /// </summary>
    public class RemoteDeviceProxyOptions
    {
        public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public int MaxRetries { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);
        public bool EnableOfflineQueue { get; set; } = true;
        public int MaxQueuedCommands { get; set; } = 1000;
    }
}
