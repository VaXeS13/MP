using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using MP.HttpApi.Hubs;
using MP.Application.Contracts.Devices;

namespace MP.HttpApi.Devices
{
    public class SignalRDeviceProxy : ITerminalProxy, IFiscalPrinterProxy, ITransientDependency, IDisposable
    {
        private readonly IHubContext<LocalAgentHub> _hubContext;
        private readonly ICurrentTenant _currentTenant;
        private readonly ILogger<SignalRDeviceProxy> _logger;

        private static readonly ConcurrentDictionary<Guid, RemoteCommand> _commandQueue = new();
        private static readonly ConcurrentDictionary<Guid, TaskCompletionSource<string?>> _responseHandlers = new();

        private const int PendingResponseCleanupIntervalSeconds = 60; // Cleanup every 1 minute
        private const int PendingResponseExpirationMinutes = 5; // Remove responses older than 5 minutes

        private Timer? _cleanupTimer;
        private bool _disposed;

        public SignalRDeviceProxy(IHubContext<LocalAgentHub> hubContext, ICurrentTenant currentTenant, ILogger<SignalRDeviceProxy> logger)
        {
            _hubContext = hubContext;
            _currentTenant = currentTenant;
            _logger = logger;

            // Initialize cleanup timer for pending responses to prevent memory leaks
            InitializeCleanupTimer();
        }

        private void InitializeCleanupTimer()
        {
            _cleanupTimer = new Timer(
                CleanupExpiredResponses,
                null,
                TimeSpan.FromSeconds(PendingResponseCleanupIntervalSeconds),
                TimeSpan.FromSeconds(PendingResponseCleanupIntervalSeconds));

            _logger.LogInformation("Memory leak cleanup timer initialized - runs every {Seconds} seconds", PendingResponseCleanupIntervalSeconds);
        }

        private void CleanupExpiredResponses(object? state)
        {
            try
            {
                var expirationThreshold = DateTime.UtcNow.AddMinutes(-PendingResponseExpirationMinutes);
                var expiredKeys = new List<Guid>();

                // Find all expired pending responses
                foreach (var kvp in _commandQueue)
                {
                    if (kvp.Value.CreatedAt < expirationThreshold)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }

                // Remove expired entries from both dictionaries
                foreach (var key in expiredKeys)
                {
                    _commandQueue.TryRemove(key, out _);
                    _responseHandlers.TryRemove(key, out _);

                    _logger.LogWarning("Cleaned up expired pending response {CommandId}", key);
                }

                if (expiredKeys.Count > 0)
                {
                    _logger.LogInformation("Memory leak cleanup complete: removed {Count} expired responses (pending: {Pending})",
                        expiredKeys.Count, _commandQueue.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during memory leak cleanup of expired responses");
            }
        }

    private async Task<T?> ExecuteCommandAsync<T>(string commandType, string deviceType, object payload, int timeoutSeconds = 30, CancellationToken cancellationToken = default) where T : class
    {
        var tenantId = _currentTenant.Id ?? throw new InvalidOperationException("Tenant context required");
        var commandId = Guid.NewGuid();

        try
        {
            var remoteCommand = new RemoteCommand { Id = commandId, CommandType = commandType, DeviceType = deviceType, Payload = JsonSerializer.Serialize(payload), TimeoutSeconds = timeoutSeconds, TenantId = tenantId, Status = RemoteCommandStatus.Pending };
            remoteCommand.UpdateExpiration();
            _commandQueue.TryAdd(commandId, remoteCommand);

            var responseSource = new TaskCompletionSource<string?>();
            _responseHandlers.TryAdd(commandId, responseSource);

            _logger.LogInformation("Executing remote command {CommandId} ({CommandType})", commandId, commandType);

            remoteCommand.Status = RemoteCommandStatus.Sent;
            await _hubContext.Clients.Group($"tenant_{tenantId}").SendAsync("ExecuteCommand", remoteCommand, cancellationToken);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds + 5));

            var response = await responseSource.Task.ConfigureAwait(false);
            return response == null ? null : JsonSerializer.Deserialize<T>(response);
        }
        finally
        {
            _responseHandlers.TryRemove(commandId, out _);
        }
    }

    public void HandleCommandResponse(Guid commandId, string response, bool isSuccess)
    {
        if (_responseHandlers.TryRemove(commandId, out var responseSource))
            responseSource.SetResult(isSuccess ? response : null);
    }

    public async Task<TerminalPaymentResult> AuthorizePaymentAsync(TerminalPaymentRequest request, CancellationToken cancellationToken = default)
    {
        // Optimized timeout: 15s for terminal payment (UX improvement)
        var timeout = Math.Min(request.TimeoutSeconds, 15);
        var result = await ExecuteCommandAsync<TerminalPaymentResult>("AuthorizePayment", "Terminal", request, timeout, cancellationToken);
        return result ?? new TerminalPaymentResult { IsSuccess = false, ErrorMessage = "No response from terminal" };
    }

    public async Task<TerminalPaymentResult> CapturePaymentAsync(string transactionId, decimal amount, CancellationToken cancellationToken = default)
    {
        // Optimized timeout: 15s for terminal operations
        var result = await ExecuteCommandAsync<TerminalPaymentResult>("CapturePayment", "Terminal", new { TransactionId = transactionId, Amount = amount }, 15, cancellationToken);
        return result ?? new TerminalPaymentResult { IsSuccess = false, ErrorMessage = "No response from terminal" };
    }

    public async Task<bool> VoidPaymentAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        // Optimized timeout: 15s for terminal operations
        var result = await ExecuteCommandAsync<dynamic>("VoidPayment", "Terminal", new { TransactionId = transactionId }, 15, cancellationToken);
        return result != null;
    }

    public async Task<bool> CheckDeviceAvailabilityAsync(string deviceType, CancellationToken cancellationToken = default)
    {
        try { return await ExecuteCommandAsync<dynamic>("CheckAvailability", deviceType, new { }, 5, cancellationToken) != null; }
        catch { return false; }
    }

    public async Task<RemoteDeviceStatus> GetDeviceStatusAsync(string deviceType, CancellationToken cancellationToken = default)
    {
        var result = await ExecuteCommandAsync<RemoteDeviceStatus>("GetStatus", deviceType, new { }, 10, cancellationToken);
        return result ?? new RemoteDeviceStatus { DeviceType = deviceType, IsOnline = false };
    }

    public async Task<TerminalCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteCommandAsync<TerminalCapabilities>("GetCapabilities", "Terminal", new { }, 10, cancellationToken) ?? new TerminalCapabilities();
    }

    public async Task<TerminalPaymentResult> RefundAsync(string originalTransactionId, decimal? amount = null, CancellationToken cancellationToken = default)
    {
        // Optimized timeout: 15s for terminal operations
        var result = await ExecuteCommandAsync<TerminalPaymentResult>("Refund", "Terminal", new { TransactionId = originalTransactionId, Amount = amount }, 15, cancellationToken);
        return result ?? new TerminalPaymentResult { IsSuccess = false, ErrorMessage = "No response from terminal" };
    }

    public async Task<FiscalReceiptResult> PrintFiscalReceiptAsync(FiscalReceiptRequest request, CancellationToken cancellationToken = default)
    {
        // Optimized timeout: 10s for fiscal printer operations (UX improvement)
        var timeout = Math.Min(request.TimeoutSeconds, 10);
        var result = await ExecuteCommandAsync<FiscalReceiptResult>("PrintReceipt", "FiscalPrinter", request, timeout, cancellationToken);
        return result ?? new FiscalReceiptResult { IsSuccess = false, ErrorMessage = "No response from fiscal printer" };
    }

    public async Task<FiscalPrinterDailySummary> GetDailySummaryAsync(CancellationToken cancellationToken = default)
    {
        // Optimized timeout: 10s for fiscal printer operations
        return await ExecuteCommandAsync<FiscalPrinterDailySummary>("GetDailySummary", "FiscalPrinter", new { }, 10, cancellationToken) ?? new FiscalPrinterDailySummary();
    }

    public async Task<string?> PrintTestReceiptAsync(CancellationToken cancellationToken = default)
    {
        // Optimized timeout: 10s for fiscal printer operations
        var result = await ExecuteCommandAsync<FiscalReceiptResult>("PrintTestReceipt", "FiscalPrinter", new { }, 10, cancellationToken);
        return result?.ReceiptId;
    }

    /// <summary>
    /// Get count of pending responses (for monitoring/debugging memory leaks)
    /// </summary>
    public int GetPendingResponseCount() => _responseHandlers.Count;

    /// <summary>
    /// Dispose resources - stop cleanup timer
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Stop and dispose the cleanup timer
            if (_cleanupTimer != null)
            {
                _cleanupTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _cleanupTimer.Dispose();
                _logger.LogInformation("Memory leak cleanup timer disposed");
            }
        }

        _disposed = true;
    }

    ~SignalRDeviceProxy()
    {
        Dispose(false);
    }

    public class RemoteCommand
    {
        public Guid Id { get; set; }
        public string CommandType { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public RemoteCommandStatus Status { get; set; } = RemoteCommandStatus.Pending;
        public int AttemptCount { get; set; } = 0;
        public string? Response { get; set; }
        public string? ErrorMessage { get; set; }
        public Guid TenantId { get; set; }

        public void UpdateExpiration() => ExpiresAt = CreatedAt.AddSeconds(TimeoutSeconds);
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        public bool CanRetry => AttemptCount < 3 && !IsExpired;
    }

    public enum RemoteCommandStatus
    {
        Pending = 0,
        Sent = 1,
        Completed = 2,
        Failed = 3,
        Timeout = 4,
        Cancelled = 5,
        Queued = 6
    }
}
}
