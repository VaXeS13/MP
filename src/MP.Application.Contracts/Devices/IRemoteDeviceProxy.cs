using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MP.Application.Contracts.Devices;

/// <summary>
/// Proxy for remote communication with physical devices via local agents through SignalR
/// </summary>
public interface IRemoteDeviceProxy
{
    /// <summary>
    /// Authorize payment on remote terminal
    /// </summary>
    /// <param name="request">Payment request with amount and details</param>
    /// <param name="cancellationToken">Cancellation token for timeout handling</param>
    /// <returns>Terminal payment result with transaction ID and status</returns>
    Task<TerminalPaymentResult> AuthorizePaymentAsync(
        TerminalPaymentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Capture previously authorized payment
    /// </summary>
    /// <param name="transactionId">ID of authorized transaction</param>
    /// <param name="amount">Amount to capture (must match or be less than authorized)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Terminal payment result with capture details</returns>
    Task<TerminalPaymentResult> CapturePaymentAsync(
        string transactionId,
        decimal amount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Void/Cancel authorized payment
    /// </summary>
    /// <param name="transactionId">ID of transaction to void</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success indicator</returns>
    Task<bool> VoidPaymentAsync(
        string transactionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Print fiscal receipt on remote fiscal printer
    /// </summary>
    /// <param name="request">Receipt request with items and amounts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Fiscal receipt result with receipt ID and status</returns>
    Task<FiscalReceiptResult> PrintFiscalReceiptAsync(
        FiscalReceiptRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if device is available and online
    /// </summary>
    /// <param name="deviceType">Type of device to check (Terminal, FiscalPrinter)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if device is available, false otherwise</returns>
    Task<bool> CheckDeviceAvailabilityAsync(
        string deviceType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current status of remote device
    /// </summary>
    /// <param name="deviceType">Type of device to query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Device status information</returns>
    Task<RemoteDeviceStatus> GetDeviceStatusAsync(
        string deviceType,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Specialized proxy interface for terminal operations
/// </summary>
public interface ITerminalProxy : IRemoteDeviceProxy
{
    /// <summary>
    /// Get terminal capabilities (card types, networks supported)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Terminal capabilities</returns>
    Task<TerminalCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Perform refund on terminal
    /// </summary>
    /// <param name="originalTransactionId">Original transaction to refund</param>
    /// <param name="amount">Refund amount (null = full refund)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Refund result</returns>
    Task<TerminalPaymentResult> RefundAsync(
        string originalTransactionId,
        decimal? amount = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Specialized proxy interface for fiscal printer operations
/// </summary>
public interface IFiscalPrinterProxy : IRemoteDeviceProxy
{
    /// <summary>
    /// Get fiscal printer daily summary
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Daily summary with totals and receipt count</returns>
    Task<FiscalPrinterDailySummary> GetDailySummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Test connection to fiscal printer
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test receipt ID if successful</returns>
    Task<string?> PrintTestReceiptAsync(CancellationToken cancellationToken = default);
}
